using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common;

namespace Client
{
    class Program
    {
        private static Socket _tcpSocket;
        private static Enkriptor _enkriptor;
        private static string _enkripcioniKljuc;
        private static int? _prijavljeniKorisnikId = null;
        private static bool _aktivan = true;

        private static string _filijalaIP = "127.0.0.1";
        private static int _filijalaPort = 6001;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== KLIJENT BANKE ===\n");

            if (args.Length >= 1)
            {
                int.TryParse(args[0], out _filijalaPort);
            }
            else
            {
                Console.Write("Unesite port filijale (default 6001): ");
                string portInput = Console.ReadLine();
                if (!string.IsNullOrEmpty(portInput))
                    int.TryParse(portInput, out _filijalaPort);
                if (_filijalaPort == 0) _filijalaPort = 6001;
            }

            try
            {

                Console.WriteLine($"Povezivanje na filijalu {_filijalaIP}:{_filijalaPort}...");
                _tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _tcpSocket.Connect(new IPEndPoint(IPAddress.Parse(_filijalaIP), _filijalaPort));
                Console.WriteLine("Povezano na filijalu!");


                PrimiKljuc();

                while (_aktivan)
                {
                    PrikaziMeni();
                    ObradiIzbor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska: {ex.Message}");
            }
            finally
            {
                _tcpSocket?.Close();
                Console.WriteLine("Klijent zatvoren.");
            }
        }

        static void PrimiKljuc()
        {
            try
            {

                _tcpSocket.ReceiveTimeout = 5000;
                byte[] buffer = new byte[4096];
                int bytesRead = _tcpSocket.Receive(buffer);

                if (bytesRead > 0)
                {
                    string porukaStr = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Poruka poruka = Serijalizator.DeserijalizujIzStringa<Poruka>(porukaStr);

                    if (poruka.Tip == TipPoruke.DistribucijaKljuca)
                    {
                        _enkripcioniKljuc = poruka.Sadrzaj;
                        _enkriptor = new Enkriptor(_enkripcioniKljuc);
                        Console.WriteLine("Enkripcioni kljuc primljen.\n");
                    }
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Kljuc nije primljen. Nastavlja se bez enkripcije.\n");
            }
            finally
            {
                _tcpSocket.ReceiveTimeout = 0;
            }
        }

        static void PrikaziMeni()
        {
            Console.WriteLine("\n--- MENI ---");

            if (_prijavljeniKorisnikId == null)
            {
                Console.WriteLine("1. Prijava");
                Console.WriteLine("2. Registracija novog korisnika");
            }
            else
            {
                Console.WriteLine($"Prijavljeni ste (ID: {_prijavljeniKorisnikId})");
                Console.WriteLine("1. Pregled stanja");
                Console.WriteLine("2. Uplata");
                Console.WriteLine("3. Isplata");
                Console.WriteLine("4. Transfer");
                Console.WriteLine("5. Odjava");
            }
            Console.WriteLine("0. Izlaz");
            Console.Write("\nIzbor: ");
        }

        static void ObradiIzbor()
        {
            string izbor = Console.ReadLine();

            if (_prijavljeniKorisnikId == null)
            {
                switch (izbor)
                {
                    case "1": Prijava(); break;
                    case "2": Registracija(); break;
                    case "0": _aktivan = false; break;
                    default: Console.WriteLine("Nepoznat izbor."); break;
                }
            }
            else
            {
                switch (izbor)
                {
                    case "1": PregledStanja(); break;
                    case "2": Uplata(); break;
                    case "3": Isplata(); break;
                    case "4": Transfer(); break;
                    case "5": Odjava(); break;
                    case "0": _aktivan = false; break;
                    default: Console.WriteLine("Nepoznat izbor."); break;
                }
            }
        }

        static void Prijava()
        {
            Console.Write("Unesite ime: ");
            string ime = Console.ReadLine();
            Console.Write("Unesite lozinku: ");
            string lozinka = Console.ReadLine();

            Poruka zahtev = new Poruka(TipPoruke.Prijava, $"{ime}:{lozinka}");
            Poruka odgovor = PosaljiIprimiOdgovor(zahtev);

            if (odgovor.Tip == TipPoruke.Uspeh)
            {
                _prijavljeniKorisnikId = odgovor.KorisnikId ?? int.Parse(odgovor.Sadrzaj);
                Console.WriteLine($"Prijava uspesna! ID: {_prijavljeniKorisnikId}");
            }
            else
            {
                Console.WriteLine($"Greska: {odgovor.Sadrzaj}");
            }
        }

        static void Registracija()
        {
            Console.Write("Unesite ime: ");
            string ime = Console.ReadLine();
            Console.Write("Unesite prezime: ");
            string prezime = Console.ReadLine();
            Console.Write("Unesite lozinku: ");
            string lozinka = Console.ReadLine();
            Console.Write("Unesite pocetno stanje: ");
            decimal.TryParse(Console.ReadLine(), out decimal pocetnoStanje);

            Poruka zahtev = new Poruka(TipPoruke.Registracija, $"{ime}:{prezime}:{lozinka}:{pocetnoStanje}");
            Poruka odgovor = PosaljiIprimiOdgovor(zahtev);

            Console.WriteLine(odgovor.Tip == TipPoruke.Uspeh
                ? $"Registracija uspesna! {odgovor.Sadrzaj}"
                : $"Greska: {odgovor.Sadrzaj}");
        }

        static void PregledStanja()
        {
            Poruka zahtev = new Poruka(TipPoruke.PregledStanja)
            {
                KorisnikId = _prijavljeniKorisnikId
            };

            Poruka odgovor = PosaljiIprimiOdgovor(zahtev);

            if (odgovor.Tip == TipPoruke.StanjeOdgovor || odgovor.Tip == TipPoruke.Uspeh)
                Console.WriteLine($"\nVase stanje: {odgovor.Sadrzaj} RSD");
            else
                Console.WriteLine($"Greska: {odgovor.Sadrzaj}");
        }

        static void Uplata()
        {
            Console.Write("Unesite iznos za uplatu: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal iznos))
            {
                Console.WriteLine("Neispravan iznos.");
                return;
            }

            Poruka zahtev = new Poruka(TipPoruke.Uplata)
            {
                KorisnikId = _prijavljeniKorisnikId,
                Iznos = iznos
            };

            Poruka odgovor = PosaljiIprimiOdgovor(zahtev);
            Console.WriteLine(odgovor.Tip == TipPoruke.Uspeh
                ? $"Uspeh: {odgovor.Sadrzaj}"
                : $"Greska: {odgovor.Sadrzaj}");
        }

        static void Isplata()
        {
            Console.Write("Unesite iznos za isplatu: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal iznos))
            {
                Console.WriteLine("Neispravan iznos.");
                return;
            }

            Poruka zahtev = new Poruka(TipPoruke.Isplata)
            {
                KorisnikId = _prijavljeniKorisnikId,
                Iznos = iznos
            };

            Poruka odgovor = PosaljiIprimiOdgovor(zahtev);
            Console.WriteLine(odgovor.Tip == TipPoruke.Uspeh
                ? $"Uspeh: {odgovor.Sadrzaj}"
                : $"Greska: {odgovor.Sadrzaj}");
        }

        static void Transfer()
        {
            Console.Write("Unesite ID primaoca: ");
            if (!int.TryParse(Console.ReadLine(), out int primalacId))
            {
                Console.WriteLine("Neispravan ID.");
                return;
            }

            Console.Write("Unesite iznos za transfer: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal iznos))
            {
                Console.WriteLine("Neispravan iznos.");
                return;
            }

            Poruka zahtev = new Poruka(TipPoruke.Transfer)
            {
                KorisnikId = _prijavljeniKorisnikId,
                PrimalacId = primalacId,
                Iznos = iznos
            };

            Poruka odgovor = PosaljiIprimiOdgovor(zahtev);
            Console.WriteLine(odgovor.Tip == TipPoruke.Uspeh
                ? $"Uspeh: {odgovor.Sadrzaj}"
                : $"Greska: {odgovor.Sadrzaj}");
        }

        static void Odjava()
        {
            _prijavljeniKorisnikId = null;
            Console.WriteLine("Odjavljeni ste.");
        }

        static Poruka PosaljiIprimiOdgovor(Poruka zahtev)
        {
            try
            {
                // Serijalizuj i sifruj
                string porukaStr = Serijalizator.SerijalizujUString(zahtev);

                if (_enkriptor != null)
                {
                    porukaStr = _enkriptor.Sifruj(porukaStr);
                }

                byte[] podaci = Encoding.UTF8.GetBytes(porukaStr);
                _tcpSocket.Send(podaci);


                byte[] buffer = new byte[4096];
                int pokusaji = 0;
                while (pokusaji < 100)
                {
                    if (_tcpSocket.Poll(100000, SelectMode.SelectRead))
                    {
                        int bytesRead = _tcpSocket.Receive(buffer);
                        if (bytesRead > 0)
                        {
                            string odgovorStr = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                            if (_enkriptor != null)
                            {
                                odgovorStr = _enkriptor.Desifruj(odgovorStr);
                            }

                            return Serijalizator.DeserijalizujIzStringa<Poruka>(odgovorStr);
                        }
                    }
                    pokusaji++;
                }

                return new Poruka(TipPoruke.Greska, "Timeout - filijala ne odgovara");
            }
            catch (Exception ex)
            {
                return new Poruka(TipPoruke.Greska, ex.Message);
            }
        }
    }
}
