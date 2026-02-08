using System;

namespace Common
{
    public enum TipTransakcije
    {
        Uplata,
        Isplata,
        Transfer
    }

    [Serializable]
    public class Transakcija
    {
        public int Id { get; set; }
        public TipTransakcije Tip { get; set; }
        public decimal Iznos { get; set; }
        public DateTime Datum { get; set; }
        public int KorisnikId { get; set; }
        public int? PrimalacId { get; set; }

        public Transakcija()
        {
            Datum = DateTime.Now;
        }

        public Transakcija(int id, TipTransakcije tip, decimal iznos, int korisnikId, int? primalacId = null)
        {
            Id = id;
            Tip = tip;
            Iznos = iznos;
            Datum = DateTime.Now;
            KorisnikId = korisnikId;
            PrimalacId = primalacId;
        }

        public override string ToString()
        {
            string opis = Tip == TipTransakcije.Transfer
                ? $"{Tip}: {Iznos:N2} RSD od korisnika {KorisnikId} ka korisniku {PrimalacId}"
                : $"{Tip}: {Iznos:N2} RSD za korisnika {KorisnikId}";
            return $"[{Id}] {opis} - {Datum:dd.MM.yyyy HH:mm:ss}";
        }
    }
}
