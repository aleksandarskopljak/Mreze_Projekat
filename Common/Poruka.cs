using System;

namespace Common
{
    public enum TipPoruke
    {

        Prijava,
        PregledStanja,
        Odjava,


        Registracija,
        Uplata,
        Isplata,
        Transfer,
        ProveriStanje,
        DohvatiKorisnika,


        Uspeh,
        Greska,
        StanjeOdgovor,
        KorisnikOdgovor,


        DistribucijaKljuca
    }

    [Serializable]
    public class Poruka
    {
        public TipPoruke Tip { get; set; }
        public string Sadrzaj { get; set; }
        public int? KorisnikId { get; set; }
        public decimal? Iznos { get; set; }
        public int? PrimalacId { get; set; }
        public int FilijalaId { get; set; }

        public Poruka()
        {
        }

        public Poruka(TipPoruke tip, string sadrzaj = null)
        {
            Tip = tip;
            Sadrzaj = sadrzaj;
        }

        public override string ToString()
        {
            return $"[{Tip}] {Sadrzaj ?? ""}";
        }
    }
}
