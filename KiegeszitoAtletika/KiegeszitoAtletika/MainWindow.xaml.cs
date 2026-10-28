
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Atletika
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Versenyzo> Versenyzok { get; set; }

        private ICollectionView nezet;

        public MainWindow()
        {
            InitializeComponent();

            Versenyzok = new ObservableCollection<Versenyzo>();
            DataContext = this;

            nezet = CollectionViewSource.GetDefaultView(Versenyzok);
            nezet.Filter = SzuroFeltetel;

            SzamlaloFrissit();
        }

        private void Eletkor_Valtozott(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtEletkor != null)
                txtEletkor.Text = ((int)sldEletkor.Value) + " év";
        }

        private bool SzuroFeltetel(object obj)
        {
            if (!(obj is Versenyzo v))
                return false;

            if (cmbRekordSzuro == null || cmbRekordSzuro.SelectedIndex == 0)
                return true;

            if (cmbRekordSzuro.SelectedIndex == 1)
                return v.OR;

            if (cmbRekordSzuro.SelectedIndex == 2)
                return v.VR;

            if (cmbRekordSzuro.SelectedIndex == 3)
                return v.ER;

            return true;
        }

        private void Szuro_Valtozott(object sender, SelectionChangedEventArgs e)
        {
            if (nezet != null)
            {
                nezet.Refresh();
                SzamlaloFrissit();
            }
        }

        private void SzamlaloFrissit()
        {
            if (txtLetszam != null && nezet != null)
                txtLetszam.Text = "Látható: " + nezet.Cast<object>().Count()
                    + " / Összesen: " + Versenyzok.Count;
        }

        private void Regisztral_Click(object sender, RoutedEventArgs e)
        {
            string nev = txtNev.Text.Trim();

            if (nev == "")
            {
                MessageBox.Show("Adja meg a versenyző nevét!");
                return;
            }

            if (nev.Contains(";") || nev.Contains("\n") || nev.Contains("\r"))
            {
                MessageBox.Show("A név nem tartalmazhat pontosvesszőt vagy sortörést.");
                return;
            }

            if (cmbVersenyszam.SelectedItem == null)
            {
                MessageBox.Show("Válasszon versenyszámot!");
                return;
            }

            if (rbFerfi.IsChecked != true && rbNo.IsChecked != true)
            {
                MessageBox.Show("Válassza ki a versenyző nemét!");
                return;
            }

            int rajtszam;

            if (txtRajtszam.Text.Trim() == "")
            {
                rajtszam = 1;

                while (Versenyzok.Any(v => v.Rajtszam == rajtszam))
                    rajtszam++;
            }
            else if (!int.TryParse(txtRajtszam.Text.Trim(), out rajtszam) || rajtszam < 1)
            {
                MessageBox.Show("A rajtszám pozitív egész szám legyen!");
                return;
            }

            if (Versenyzok.Any(v => v.Rajtszam == rajtszam))
            {
                MessageBox.Show("Ez a rajtszám már foglalt!");
                return;
            }

            string versenyszam =
                ((ComboBoxItem)cmbVersenyszam.SelectedItem).Content.ToString();

            Versenyzok.Add(new Versenyzo
            {
                Rajtszam = rajtszam,
                Nev = nev,
                Versenyszam = versenyszam,
                Nem = rbFerfi.IsChecked == true ? "F" : "N",
                Eletkor = (int)sldEletkor.Value,
                OR = chkOR.IsChecked == true,
                VR = chkVR.IsChecked == true,
                ER = chkER.IsChecked == true
            });

            nezet.Refresh();
            SzamlaloFrissit();

            txtNev.Clear();
            txtRajtszam.Clear();
            cmbVersenyszam.SelectedIndex = -1;
            rbFerfi.IsChecked = false;
            rbNo.IsChecked = false;
            sldEletkor.Value = 18;
            chkOR.IsChecked = false;
            chkVR.IsChecked = false;
            chkER.IsChecked = false;
        }

        private void Mentes_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                FileName = "atletika.csv",
                Filter = "CSV-fájl (*.csv)|*.csv|Szövegfájl (*.txt)|*.txt",
                FilterIndex = 1,
                DefaultExt = ".csv",
                AddExtension = true
            };

            if (dialog.ShowDialog() != true)
                return;

            string kiterjesztes = dialog.FilterIndex == 1 ? ".csv" : ".txt";
            string fajlnev = dialog.FileName;
            string megadottKiterjesztes = Path.GetExtension(fajlnev);

            if (megadottKiterjesztes != "")
            {
                if (!megadottKiterjesztes.Equals(
                    kiterjesztes, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("A kiválasztott fájltípus és a kiterjesztés nem egyezik!");
                    return;
                }
            }
            else
            {
                fajlnev += kiterjesztes;
            }

            try
            {
                if (dialog.FilterIndex == 1)
                    CsvMentes(fajlnev);
                else
                    TxtMentes(fajlnev);

                MessageBox.Show("A mentés sikeres.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba történt a mentés során: " + ex.Message);
            }
        }

        private void CsvMentes(string fajlnev)
        {
            List<string> sorok = new List<string>
            {
                "Rajtszam;Nev;Versenyszam;Nem;Eletkor;OR;VR;ER"
            };

            foreach (Versenyzo v in Versenyzok)
            {
                sorok.Add(string.Join(";", new string[]
                {
                    v.Rajtszam.ToString(),
                    v.Nev,
                    v.Versenyszam,
                    v.Nem,
                    v.Eletkor.ToString(),
                    v.OR ? "1" : "0",
                    v.VR ? "1" : "0",
                    v.ER ? "1" : "0"
                }));
            }

            File.WriteAllLines(fajlnev, sorok, new UTF8Encoding(false));
        }

        private void TxtMentes(string fajlnev)
        {
            List<string> sorok = new List<string>();

            foreach (Versenyzo v in Versenyzok)
            {
                string nem = v.Nem == "F" ? "férfi" : "nő";

                sorok.Add(v.Nev);
                sorok.Add(
                    "A " + v.Eletkor + " éves " + nem +
                    " versenyző a(z) " + v.Versenyszam +
                    " versenyszámban indul, rajtszáma " + v.Rajtszam +
                    ". Olimpiai csúccsal " + (v.OR ? "rendelkezik" : "nem rendelkezik") +
                    ", világcsúccsal " + (v.VR ? "rendelkezik" : "nem rendelkezik") +
                    ", Európa-csúccsal " + (v.ER ? "rendelkezik" : "nem rendelkezik") + ".");
                sorok.Add("");
            }

            if (sorok.Count > 0)
                sorok.RemoveAt(sorok.Count - 1);

            File.WriteAllLines(fajlnev, sorok, new UTF8Encoding(false));
        }

        private void Betoltes_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "CSV-fájl (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string[] sorok = File.ReadAllLines(dialog.FileName, Encoding.UTF8);

                if (sorok.Length == 0 ||
                    sorok[0].TrimStart('\uFEFF') !=
                    "Rajtszam;Nev;Versenyszam;Nem;Eletkor;OR;VR;ER")
                {
                    MessageBox.Show("A CSV fejlécformátuma nem megfelelő!");
                    return;
                }

                List<Versenyzo> ideiglenes = new List<Versenyzo>();
                HashSet<int> rajtszamok = new HashSet<int>();

                for (int i = 1; i < sorok.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(sorok[i]))
                        continue;

                    string[] m = sorok[i].Split(';');

                    if (m.Length != 8)
                        throw new Exception("Hibás mezőszám a(z) " + (i + 1) + ". sorban.");

                    if (!int.TryParse(m[0], out int rajtszam) || rajtszam < 1)
                        throw new Exception("Hibás rajtszám a(z) " + (i + 1) + ". sorban.");

                    if (!rajtszamok.Add(rajtszam))
                        throw new Exception("Ismétlődő rajtszám: " + rajtszam);

                    if (!int.TryParse(m[4], out int eletkor) || eletkor < 18 || eletkor > 90)
                        throw new Exception("Hibás életkor a(z) " + (i + 1) + ". sorban.");

                    if (m[3] != "F" && m[3] != "N")
                        throw new Exception("Hibás nem mező a(z) " + (i + 1) + ". sorban.");

                    if (!int.TryParse(m[5], out int or) || (or != 0 && or != 1) ||
                        !int.TryParse(m[6], out int vr) || (vr != 0 && vr != 1) ||
                        !int.TryParse(m[7], out int er) || (er != 0 && er != 1))
                        throw new Exception("Hibás rekordjelölés a(z) " + (i + 1) + ". sorban.");

                    ideiglenes.Add(new Versenyzo
                    {
                        Rajtszam = rajtszam,
                        Nev = m[1],
                        Versenyszam = m[2],
                        Nem = m[3],
                        Eletkor = eletkor,
                        OR = or == 1,
                        VR = vr == 1,
                        ER = er == 1
                    });
                }

                Versenyzok.Clear();

                foreach (Versenyzo v in ideiglenes)
                    Versenyzok.Add(v);

                nezet.Refresh();
                SzamlaloFrissit();

                MessageBox.Show("A betöltés sikeres. Rekordok száma: " + Versenyzok.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba történt a betöltés során: " + ex.Message);
            }
        }
    }
}
