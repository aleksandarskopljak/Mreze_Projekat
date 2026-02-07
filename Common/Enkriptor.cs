using System;
using System.Text;

namespace Common
{
    public class Enkriptor
    {
        private string _kljuc;
        private const string Karakteri = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,;:!?()-+=*/\\\"'@#$%^&[]{}|<>~`\r\n";

        public Enkriptor(string kljuc)
        {
            if (string.IsNullOrEmpty(kljuc))
                throw new ArgumentException("Kljuc ne moze biti prazan");
            _kljuc = kljuc;
        }

        public string Sifruj(string tekst)
        {
            if (string.IsNullOrEmpty(tekst))
                return tekst;

            StringBuilder rezultat = new StringBuilder();
            int kljucIndex = 0;

            foreach (char c in tekst)
            {
                int tekstIndex = Karakteri.IndexOf(c);
                if (tekstIndex >= 0)
                {
                    int kljucKarakterIndex = Karakteri.IndexOf(_kljuc[kljucIndex % _kljuc.Length]);
                    if (kljucKarakterIndex < 0) kljucKarakterIndex = 0;

                    int noviIndex = (tekstIndex + kljucKarakterIndex) % Karakteri.Length;
                    rezultat.Append(Karakteri[noviIndex]);
                    kljucIndex++;
                }
                else
                {
                    rezultat.Append(c);
                }
            }

            return rezultat.ToString();
        }

        public string Desifruj(string sifrovaniTekst)
        {
            if (string.IsNullOrEmpty(sifrovaniTekst))
                return sifrovaniTekst;

            StringBuilder rezultat = new StringBuilder();
            int kljucIndex = 0;

            foreach (char c in sifrovaniTekst)
            {
                int tekstIndex = Karakteri.IndexOf(c);
                if (tekstIndex >= 0)
                {
                    int kljucKarakterIndex = Karakteri.IndexOf(_kljuc[kljucIndex % _kljuc.Length]);
                    if (kljucKarakterIndex < 0) kljucKarakterIndex = 0;

                    int noviIndex = (tekstIndex - kljucKarakterIndex + Karakteri.Length) % Karakteri.Length;
                    rezultat.Append(Karakteri[noviIndex]);
                    kljucIndex++;
                }
                else
                {
                    rezultat.Append(c);
                }
            }

            return rezultat.ToString();
        }

        public void PostaviKljuc(string noviKljuc)
        {
            if (string.IsNullOrEmpty(noviKljuc))
                throw new ArgumentException("Kljuc ne moze biti prazan");
            _kljuc = noviKljuc;
        }
    }
}
