using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using Common;

namespace Server
{
    class Program
    {
        private static Socket _udpSocket;
        private static Dictionary<int, Korisnik> _korisnici = new Dictionary<int, Korisnik>();
        private static List<Transakcija> _transakcije = new List<Transakcija>();
        private static Dictionary<int, EndPoint> _filijale = new Dictionary<int, EndPoint>();
        private static Enkriptor _enkriptor;
        private static string _enkripcioniKljuc;
        private static bool _aktivan = true;
        private static int _sledecaTransakcijaId = 1;
        private static int _sledeciKorisnikId = 1;
        private static int _sledecaFilijalaId = 1;
        private static object _lock = new object();
        private static List<Process> _pokrenuitProcesi = new List<Process>();

        private static int _serverPort = 5000;
        private static string _baseDir;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== CENTRALNI SERVER BANKE ===\n");

            Console.Write("Unesite enkripcioni kljuc (Vigenere): ");
            _enkripcioniKljuc = Console.ReadLine();

            if (string.IsNullOrEmpty(_enkripcioniKljuc))
            {
                _enkripcioniKljuc = "BANKA2024";
                Console.WriteLine($"Koristi se podrazumevani kljuc: {_enkripcioniKljuc}");
            }

            _enkriptor = new Enkriptor(_enkripcioniKljuc);
            _baseDir = AppDomain.CurrentDomain.BaseDirectory;

            KreirajTestKorisnike();

            try
            {

                _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _udpSocket.Bind(new IPEndPoint(IPAddress.Any, _serverPort));
                _udpSocket.Blocking = false;
                Console.WriteLine($"UDP soket pokrenut na portu {_serverPort}");

                Console.WriteLine("\nServer je aktivan. Komande:");
                Console.WriteLine("  K - Prikazi korisnike");
                Console.WriteLine("  T - Prikazi transakcije");
                Console.WriteLine("  F - Prikazi filijale");
                Console.WriteLine("  N - Novi kljuc");
                Console.WriteLine("  1 - Pokreni novu filijalu");
                Console.WriteLine("  2 - Pokreni novog klijenta");
                Console.WriteLine("  Q - Izlaz\n");


                while (_aktivan)
                {
                    if (Console.KeyAvailable)
                    {
                        ObradiKomandu(Console.ReadKey(true).Key);
                    }


                    ProveriDolazneporuke();

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska servera: {ex.Message}");
            }
            finally
            {
                foreach (var proces in _pokrenuitProcesi)
                {
                    try
                    {
                        if (!proces.HasExited)
                            proces.Kill();
                    }
                    catch { }
                }
                _udpSocket?.Close();
                Console.WriteLine("Server ugasen.");
            }
        }

        static void KreirajTestKorisnike()
        {
            _korisnici[1] = new Korisnik(1, "Marko", "Markovic", "marko123", 10000);
            _korisnici[2] = new Korisnik(2, "Jovan", "Jovanovic", "jovan123", 25000);
            _korisnici[3] = new Korisnik(3, "Ana", "Anic", "ana123", 15000);
            _sledeciKorisnikId = 4;

            Console.WriteLine("Test korisnici kreirani:");
            foreach (var k in _korisnici.Values)
            {
                Console.WriteLine($"  {k}");
            }
            Console.WriteLine();
        }

        static void ObradiKomandu(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.K:
                    PrikaziKorisnike();
                    break;
                case ConsoleKey.T:
                    PrikaziTransakcije();
                    break;
                case ConsoleKey.F:
                    PrikaziFilijale();
                    break;
                case ConsoleKey.N:
                    PromeniKljuc();
                    break;
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    PokreniNovuFilijalu();
                    break;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    PokreniNovogKlijenta();
                    break;
                case ConsoleKey.Q:
                    _aktivan = false;
                    break;
            }
        }

        static void PokreniNovuFilijalu()
        {
            int filijalaId = _sledecaFilijalaId++;
            int tcpPort = 6000 + filijalaId;

            string filijalaPath = Path.Combine(_baseDir, "..", "..", "..", "Filijala", "bin", "Debug", "Filijala.exe");
            if (!File.Exists(filijalaPath))
                filijalaPath = Path.Combine(_baseDir, "Filijala.exe");
            if (!File.Exists(filijalaPath))
                filijalaPath = Path.GetFullPath(Path.Combine(_baseDir, "..", "Filijala", "bin", "Debug", "Filijala.exe"));

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = filijalaPath,
                    Arguments = $"{filijalaId} {tcpPort}",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                Process proces = Process.Start(psi);
                if (proces != null)
                {
                    _pokrenuitProcesi.Add(proces);
                    Console.WriteLine($"\nFilijala {filijalaId} pokrenuta na TCP portu {tcpPort}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nGreska pri pokretanju filijale: {ex.Message}");
            }
        }

        static void PokreniNovogKlijenta()
        {
            Console.Write("\nUnesite TCP port filijale za povezivanje (npr. 6001): ");
            string portStr = Console.ReadLine();
            if (!int.TryParse(portStr, out int port))
                port = 6001;

            string clientPath = Path.Combine(_baseDir, "..", "..", "..", "Client", "bin", "Debug", "Client.exe");
            if (!File.Exists(clientPath))
                clientPath = Path.Combine(_baseDir, "Client.exe");
            if (!File.Exists(clientPath))
                clientPath = Path.GetFullPath(Path.Combine(_baseDir, "..", "Client", "bin", "Debug", "Client.exe"));

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = clientPath,
                    Arguments = port.ToString(),
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                Process proces = Process.Start(psi);
                if (proces != null)
                {
                    _pokrenuitProcesi.Add(proces);
                    Console.WriteLine($"Klijent pokrenut, povezuje se na port {port}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nGreska pri pokretanju klijenta: {ex.Message}");
            }
        }

        static void PrikaziKorisnike()
        {
            Console.WriteLine("\n--- KORISNICI ---");
            lock (_lock)
            {
                foreach (var k in _korisnici.Values)
                    Console.WriteLine($"  {k}");
            }
            Console.WriteLine();
        }

        static void PrikaziTransakcije()
        {
            Console.WriteLine("\n--- TRANSAKCIJE ---");
            lock (_lock)
            {
                if (_transakcije.Count == 0)
                    Console.WriteLine("  Nema transakcija.");
                else
                    foreach (var t in _transakcije.Skip(Math.Max(0, _transakcije.Count - 20)))
                        Console.WriteLine($"  {t}");
            }
            Console.WriteLine();
        }

        static void PrikaziFilijale()
        {
            Console.WriteLine("\n--- POVEZANE FILIJALE ---");
            lock (_lock)
            {
                if (_filijale.Count == 0)
                    Console.WriteLine("  Nema povezanih filijala.");
                else
                    foreach (var f in _filijale)
                        Console.WriteLine($"  Filijala {f.Key}: {f.Value}");
            }
            Console.WriteLine();
        }

        static void PromeniKljuc()
        {
            Console.Write("\nUnesite novi enkripcioni kljuc: ");
            string noviKljuc = Console.ReadLine();

            if (!string.IsNullOrEmpty(noviKljuc))
            {
                _enkripcioniKljuc = noviKljuc;
                _enkriptor = new Enkriptor(_enkripcioniKljuc);
                Console.WriteLine("Kljuc promenjen. Distribucija filijalama...");
                DistribuirajKljuc();
            }
        }

        static void DistribuirajKljuc()
        {
            Poruka kljucPoruka = new Poruka(TipPoruke.DistribucijaKljuca, _enkripcioniKljuc);

            lock (_lock)
            {
                foreach (var filijala in _filijale)
                {
                    PosaljiFilijali(filijala.Value, kljucPoruka, false);
                }
            }
        }

        static void ProveriDolazneporuke()
        {
            try
            {

                if (_udpSocket.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buffer = new byte[65535];
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    int primljeno = _udpSocket.ReceiveFrom(buffer, ref remoteEP);

                    if (primljeno > 0)
                    {
                        string porukaStr = Encoding.UTF8.GetString(buffer, 0, primljeno);

                        Poruka poruka = null;
                        try
                        {
                            poruka = Serijalizator.DeserijalizujIzStringa<Poruka>(porukaStr);
                        }
                        catch
                        {
                            try
                            {
                                string desifrovano = _enkriptor.Desifruj(porukaStr);
                                poruka = Serijalizator.DeserijalizujIzStringa<Poruka>(desifrovano);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Greska pri deserijalizaciji: {ex.Message}");
                                return;
                            }
                        }

                        if (poruka != null)
                        {
                            ObradiPoruku(poruka, remoteEP);
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode != SocketError.WouldBlock)
                    Console.WriteLine($"Socket greska: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska: {ex.Message}");
            }
        }

        static void ObradiPoruku(Poruka poruka, EndPoint posiljalac)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Primljeno: {poruka.Tip} od {posiljalac}");

            Poruka odgovor;

            switch (poruka.Tip)
            {
                case TipPoruke.Registracija:
                    if (poruka.Sadrzaj != null && poruka.Sadrzaj.StartsWith("Filijala_"))
                        odgovor = ObradiRegistracijuFilijale(poruka, posiljalac);
                    else
                        odgovor = ObradiRegistracijuKorisnika(poruka);
                    break;
                case TipPoruke.Prijava:
                    odgovor = ObradiPrijavuKorisnika(poruka);
                    break;
                case TipPoruke.ProveriStanje:
                    odgovor = ObradiProveriStanje(poruka);
                    break;
                case TipPoruke.Uplata:
                    odgovor = ObradiUplatu(poruka);
                    break;
                case TipPoruke.Isplata:
                    odgovor = ObradiIsplatu(poruka);
                    break;
                case TipPoruke.Transfer:
                    odgovor = ObradiTransfer(poruka);
                    break;
                case TipPoruke.DohvatiKorisnika:
                    odgovor = ObradiDohvatiKorisnika(poruka);
                    break;
                default:
                    odgovor = new Poruka(TipPoruke.Greska, "Nepoznat tip poruke");
                    break;
            }

            bool jeRegistracija = poruka.Tip == TipPoruke.Registracija;
            PosaljiFilijali(posiljalac, odgovor, !jeRegistracija);
        }

        static Poruka ObradiRegistracijuFilijale(Poruka poruka, EndPoint posiljalac)
        {
            lock (_lock)
            {
                _filijale[poruka.FilijalaId] = posiljalac;
            }
            Console.WriteLine($"Filijala {poruka.FilijalaId} registrovana: {posiljalac}");
            return new Poruka(TipPoruke.DistribucijaKljuca, _enkripcioniKljuc);
        }

        static Poruka ObradiPrijavuKorisnika(Poruka poruka)
        {
            string[] delovi = poruka.Sadrzaj?.Split(':');
            if (delovi == null || delovi.Length < 2)
                return new Poruka(TipPoruke.Greska, "Neispravan format prijave");

            string ime = delovi[0];
            string lozinka = delovi[1];

            lock (_lock)
            {
                var korisnik = _korisnici.Values.FirstOrDefault(k => k.Ime == ime && k.Lozinka == lozinka);
                if (korisnik != null)
                {
                    return new Poruka(TipPoruke.Uspeh, korisnik.Id.ToString())
                    {
                        KorisnikId = korisnik.Id
                    };
                }
            }
            return new Poruka(TipPoruke.Greska, "Pogresno korisnicko ime ili lozinka");
        }

        static Poruka ObradiProveriStanje(Poruka poruka)
        {
            if (!poruka.KorisnikId.HasValue)
                return new Poruka(TipPoruke.Greska, "Nedostaje ID korisnika");

            lock (_lock)
            {
                if (_korisnici.TryGetValue(poruka.KorisnikId.Value, out Korisnik korisnik))
                {
                    return new Poruka(TipPoruke.StanjeOdgovor, korisnik.StanjeNaRacunu.ToString("N2"))
                    {
                        KorisnikId = korisnik.Id,
                        Iznos = korisnik.StanjeNaRacunu
                    };
                }
            }
            return new Poruka(TipPoruke.Greska, "Korisnik nije pronadjen");
        }

        static Poruka ObradiUplatu(Poruka poruka)
        {
            if (!poruka.KorisnikId.HasValue || !poruka.Iznos.HasValue)
                return new Poruka(TipPoruke.Greska, "Nedostaju podaci za uplatu");
            if (poruka.Iznos.Value <= 0)
                return new Poruka(TipPoruke.Greska, "Iznos mora biti pozitivan");

            lock (_lock)
            {
                if (_korisnici.TryGetValue(poruka.KorisnikId.Value, out Korisnik korisnik))
                {
                    korisnik.StanjeNaRacunu += poruka.Iznos.Value;

                    var transakcija = new Transakcija(_sledecaTransakcijaId++, TipTransakcije.Uplata, poruka.Iznos.Value, korisnik.Id);
                    _transakcije.Add(transakcija);

                    Console.WriteLine($"  Uplata: {poruka.Iznos.Value:N2} RSD za {korisnik.Ime}. Novo stanje: {korisnik.StanjeNaRacunu:N2}");
                    return new Poruka(TipPoruke.Uspeh, $"Uplata uspesna. Novo stanje: {korisnik.StanjeNaRacunu:N2} RSD")
                    {
                        Iznos = korisnik.StanjeNaRacunu
                    };
                }
            }
            return new Poruka(TipPoruke.Greska, "Korisnik nije pronadjen");
        }

        static Poruka ObradiIsplatu(Poruka poruka)
        {
            if (!poruka.KorisnikId.HasValue || !poruka.Iznos.HasValue)
                return new Poruka(TipPoruke.Greska, "Nedostaju podaci za isplatu");
            if (poruka.Iznos.Value <= 0)
                return new Poruka(TipPoruke.Greska, "Iznos mora biti pozitivan");

            lock (_lock)
            {
                if (_korisnici.TryGetValue(poruka.KorisnikId.Value, out Korisnik korisnik))
                {
                    if (korisnik.StanjeNaRacunu < poruka.Iznos.Value)
                        return new Poruka(TipPoruke.Greska, $"Nedovoljno sredstava. Trenutno stanje: {korisnik.StanjeNaRacunu:N2} RSD");

                    korisnik.StanjeNaRacunu -= poruka.Iznos.Value;

                    var transakcija = new Transakcija(_sledecaTransakcijaId++, TipTransakcije.Isplata, poruka.Iznos.Value, korisnik.Id);
                    _transakcije.Add(transakcija);

                    Console.WriteLine($"  Isplata: {poruka.Iznos.Value:N2} RSD za {korisnik.Ime}. Novo stanje: {korisnik.StanjeNaRacunu:N2}");
                    return new Poruka(TipPoruke.Uspeh, $"Isplata uspesna. Novo stanje: {korisnik.StanjeNaRacunu:N2} RSD")
                    {
                        Iznos = korisnik.StanjeNaRacunu
                    };
                }
            }
            return new Poruka(TipPoruke.Greska, "Korisnik nije pronadjen");
        }

        static Poruka ObradiTransfer(Poruka poruka)
        {
            if (!poruka.KorisnikId.HasValue || !poruka.PrimalacId.HasValue || !poruka.Iznos.HasValue)
                return new Poruka(TipPoruke.Greska, "Nedostaju podaci za transfer");
            if (poruka.Iznos.Value <= 0)
                return new Poruka(TipPoruke.Greska, "Iznos mora biti pozitivan");
            if (poruka.KorisnikId.Value == poruka.PrimalacId.Value)
                return new Poruka(TipPoruke.Greska, "Ne mozete slati sredstva samom sebi");

            lock (_lock)
            {
                if (!_korisnici.TryGetValue(poruka.KorisnikId.Value, out Korisnik posiljalac))
                    return new Poruka(TipPoruke.Greska, "Posiljalac nije pronadjen");
                if (!_korisnici.TryGetValue(poruka.PrimalacId.Value, out Korisnik primalac))
                    return new Poruka(TipPoruke.Greska, "Primalac nije pronadjen");

                if (posiljalac.StanjeNaRacunu < poruka.Iznos.Value)
                    return new Poruka(TipPoruke.Greska, $"Nedovoljno sredstava. Trenutno stanje: {posiljalac.StanjeNaRacunu:N2} RSD");

                posiljalac.StanjeNaRacunu -= poruka.Iznos.Value;
                primalac.StanjeNaRacunu += poruka.Iznos.Value;

                var transakcija = new Transakcija(_sledecaTransakcijaId++, TipTransakcije.Transfer, poruka.Iznos.Value, posiljalac.Id, primalac.Id);
                _transakcije.Add(transakcija);

                Console.WriteLine($"  Transfer: {poruka.Iznos.Value:N2} RSD od {posiljalac.Ime} ka {primalac.Ime}");
                return new Poruka(TipPoruke.Uspeh,
                    $"Transfer uspesan. Poslato {poruka.Iznos.Value:N2} RSD korisniku {primalac.Ime}. Vase stanje: {posiljalac.StanjeNaRacunu:N2} RSD")
                {
                    Iznos = posiljalac.StanjeNaRacunu
                };
            }
        }

        static Poruka ObradiRegistracijuKorisnika(Poruka poruka)
        {
            string[] delovi = poruka.Sadrzaj?.Split(':');
            if (delovi == null || delovi.Length < 4)
                return new Poruka(TipPoruke.Greska, "Neispravan format registracije");

            string ime = delovi[0];
            string prezime = delovi[1];
            string lozinka = delovi[2];
            if (!decimal.TryParse(delovi[3], out decimal pocetnoStanje))
                pocetnoStanje = 0;

            lock (_lock)
            {
                var postojeci = _korisnici.Values.FirstOrDefault(k => k.Ime == ime && k.Prezime == prezime);
                if (postojeci != null)
                    return new Poruka(TipPoruke.Greska, "Korisnik sa tim imenom vec postoji");

                int noviId = _sledeciKorisnikId++;
                var noviKorisnik = new Korisnik(noviId, ime, prezime, lozinka, pocetnoStanje);
                _korisnici[noviId] = noviKorisnik;

                Console.WriteLine($"  Novi korisnik registrovan: {noviKorisnik}");
                return new Poruka(TipPoruke.Uspeh, $"Registracija uspesna. Vas ID je {noviId}.")
                {
                    KorisnikId = noviId
                };
            }
        }

        static Poruka ObradiDohvatiKorisnika(Poruka poruka)
        {
            if (!poruka.KorisnikId.HasValue)
                return new Poruka(TipPoruke.Greska, "Nedostaje ID korisnika");

            lock (_lock)
            {
                if (_korisnici.TryGetValue(poruka.KorisnikId.Value, out Korisnik korisnik))
                {
                    string korisnikInfo = $"{korisnik.Id}:{korisnik.Ime}:{korisnik.Prezime}:{korisnik.StanjeNaRacunu}";
                    return new Poruka(TipPoruke.KorisnikOdgovor, korisnikInfo)
                    {
                        KorisnikId = korisnik.Id
                    };
                }
            }
            return new Poruka(TipPoruke.Greska, "Korisnik nije pronadjen");
        }

        static void PosaljiFilijali(EndPoint filijalaEndPoint, Poruka poruka, bool sifruj)
        {
            string porukaStr = Serijalizator.SerijalizujUString(poruka);

            if (sifruj)
            {
                porukaStr = _enkriptor.Sifruj(porukaStr);
            }

            byte[] podaci = Encoding.UTF8.GetBytes(porukaStr);
            _udpSocket.SendTo(podaci, filijalaEndPoint);
        }
    }
}
