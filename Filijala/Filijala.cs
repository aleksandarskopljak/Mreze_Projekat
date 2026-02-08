using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common;

namespace Filijala
{
    class Program
    {
        private static int _filijalaId;
        private static Socket _udpSocket;
        private static Socket _tcpListenerSocket;
        private static EndPoint _serverEndPoint;
        private static Enkriptor _enkriptor;
        private static bool _aktivan = true;
        private static List<Socket> _povezaniKlijenti = new List<Socket>();
        private static object _lock = new object();
        private static string _enkripcioniKljuc = null;

        private static int _serverPort = 5000;
        private static string _serverIP = "127.0.0.1";
        private static int _tcpPort = 6000;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== FILIJALA BANKE ===\n");

            if (args.Length >= 1)
                int.TryParse(args[0], out _filijalaId);
            else
            {
                Console.Write("Unesite ID filijale: ");
                int.TryParse(Console.ReadLine(), out _filijalaId);
            }

            if (args.Length >= 2)
                int.TryParse(args[1], out _tcpPort);
            else
                _tcpPort = 6000 + _filijalaId;

            Console.WriteLine($"Filijala ID: {_filijalaId}");
            Console.WriteLine($"TCP port za klijente: {_tcpPort}");

            try
            {

                _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _udpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                _serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverIP), _serverPort);


                RegistrujKodServera();


                _tcpListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _tcpListenerSocket.Bind(new IPEndPoint(IPAddress.Any, _tcpPort));
                _tcpListenerSocket.Listen(10);
                _tcpListenerSocket.Blocking = false;
                Console.WriteLine($"TCP listener soket pokrenut na portu {_tcpPort}");


                Thread klijentThread = new Thread(PrihvatajKlijente);
                klijentThread.IsBackground = true;
                klijentThread.Start();

                Console.WriteLine("\nFilijala je aktivna. Pritisnite 'Q' za izlaz.\n");

                while (_aktivan)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Q)
                        {
                            _aktivan = false;
                            break;
                        }
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska: {ex.Message}");
            }
            finally
            {
                Zatvori();
            }
        }

        static void RegistrujKodServera()
        {
            Console.WriteLine("Registracija kod centralnog servera...");

            Poruka registracija = new Poruka(TipPoruke.Registracija)
            {
                FilijalaId = _filijalaId,
                Sadrzaj = $"Filijala_{_filijalaId}:{_tcpPort}"
            };

            PosaljiServeru(registracija);


            try
            {
                _udpSocket.ReceiveTimeout = 5000;
                byte[] buffer = new byte[65535];
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                int primljeno = _udpSocket.ReceiveFrom(buffer, ref remoteEP);

                string odgovorStr = Encoding.UTF8.GetString(buffer, 0, primljeno);
                Poruka odgovor = Serijalizator.DeserijalizujIzStringa<Poruka>(odgovorStr);

                if (odgovor.Tip == TipPoruke.DistribucijaKljuca)
                {
                    _enkripcioniKljuc = odgovor.Sadrzaj;
                    _enkriptor = new Enkriptor(_enkripcioniKljuc);
                    Console.WriteLine("Enkripcioni kljuc primljen od servera.");
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Server nije dostupan. Nastavlja se bez enkripcije.");
            }
            finally
            {
                _udpSocket.ReceiveTimeout = 0;
            }
        }

        static void PosaljiServeru(Poruka poruka)
        {
            string porukaStr = Serijalizator.SerijalizujUString(poruka);

            if (_enkriptor != null)
            {
                porukaStr = _enkriptor.Sifruj(porukaStr);
            }

            byte[] podaci = Encoding.UTF8.GetBytes(porukaStr);
            _udpSocket.SendTo(podaci, _serverEndPoint);
        }

        static Poruka CekajOdgovorServera()
        {
            int pokusaji = 0;
            while (pokusaji < 50)
            {
                try
                {

                    if (_udpSocket.Poll(100000, SelectMode.SelectRead))
                    {
                        byte[] buffer = new byte[65535];
                        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                        int primljeno = _udpSocket.ReceiveFrom(buffer, ref remoteEP);

                        string porukaStr = Encoding.UTF8.GetString(buffer, 0, primljeno);

                        Poruka poruka = null;


                        try
                        {
                            poruka = Serijalizator.DeserijalizujIzStringa<Poruka>(porukaStr);
                        }
                        catch
                        {
                            if (_enkriptor != null)
                            {
                                try
                                {
                                    string desifrovano = _enkriptor.Desifruj(porukaStr);
                                    poruka = Serijalizator.DeserijalizujIzStringa<Poruka>(desifrovano);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Greska pri desifrovanju: {ex.Message}");
                                    continue;
                                }
                            }
                        }

                        if (poruka != null)
                        {
                            if (poruka.Tip == TipPoruke.DistribucijaKljuca)
                            {
                                _enkripcioniKljuc = poruka.Sadrzaj;
                                _enkriptor = new Enkriptor(_enkripcioniKljuc);
                                Console.WriteLine("Novi enkripcioni kljuc primljen.");
                            }
                            return poruka;
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
                    Console.WriteLine($"Greska u CekajOdgovorServera: {ex.Message}");
                }

                pokusaji++;
            }

            return new Poruka(TipPoruke.Greska, "Server ne odgovara");
        }

        static void PrihvatajKlijente()
        {
            Console.WriteLine("Cekanje na klijente...");

            while (_aktivan)
            {
                try
                {

                    if (_tcpListenerSocket.Poll(50000, SelectMode.SelectRead))
                    {
                        Socket klijentSocket = _tcpListenerSocket.Accept();
                        Console.WriteLine($"Novi klijent povezan: {klijentSocket.RemoteEndPoint}");

                        lock (_lock)
                        {
                            _povezaniKlijenti.Add(klijentSocket);
                        }

                        Thread klijentHandler = new Thread(() => ObradiKlijenta(klijentSocket));
                        klijentHandler.IsBackground = true;
                        klijentHandler.Start();
                    }


                    List<Socket> zaUklanjanje = new List<Socket>();
                    lock (_lock)
                    {
                        foreach (var klijent in _povezaniKlijenti)
                        {
                            if (!klijent.Connected)
                                zaUklanjanje.Add(klijent);
                        }
                        foreach (var klijent in zaUklanjanje)
                        {
                            _povezaniKlijenti.Remove(klijent);
                            klijent.Close();
                        }
                    }
                }
                catch (SocketException ex)
                {
                    if (_aktivan && ex.SocketErrorCode != SocketError.WouldBlock)
                        Console.WriteLine($"Greska pri prihvatanju klijenta: {ex.Message}");
                }
                catch (Exception ex)
                {
                    if (_aktivan)
                        Console.WriteLine($"Greska: {ex.Message}");
                }
            }
        }

        static void ObradiKlijenta(Socket klijentSocket)
        {
            try
            {

                if (_enkripcioniKljuc != null)
                {
                    Poruka kljucPoruka = new Poruka(TipPoruke.DistribucijaKljuca, _enkripcioniKljuc);
                    PosaljiKlijentu(klijentSocket, kljucPoruka, false);
                }

                byte[] buffer = new byte[4096];

                while (_aktivan && klijentSocket.Connected)
                {

                    if (klijentSocket.Poll(10000, SelectMode.SelectRead))
                    {
                        int bytesRead = klijentSocket.Receive(buffer);
                        if (bytesRead == 0)
                            break;

                        string porukaStr = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        if (_enkriptor != null)
                        {
                            porukaStr = _enkriptor.Desifruj(porukaStr);
                        }

                        Poruka poruka = Serijalizator.DeserijalizujIzStringa<Poruka>(porukaStr);
                        Console.WriteLine($"Zahtev od klijenta: {poruka.Tip}");

                        Poruka odgovor = ObradiKlijentZahtev(poruka);
                        PosaljiKlijentu(klijentSocket, odgovor, true);
                    }
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode != SocketError.ConnectionReset)
                    Console.WriteLine($"Socket greska sa klijentom: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska u komunikaciji sa klijentom: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    _povezaniKlijenti.Remove(klijentSocket);
                }
                klijentSocket.Close();
                Console.WriteLine("Klijent disconnected.");
            }
        }

        static void PosaljiKlijentu(Socket klijentSocket, Poruka poruka, bool sifruj)
        {
            string porukaStr = Serijalizator.SerijalizujUString(poruka);

            if (sifruj && _enkriptor != null)
            {
                porukaStr = _enkriptor.Sifruj(porukaStr);
            }

            byte[] podaci = Encoding.UTF8.GetBytes(porukaStr);
            klijentSocket.Send(podaci);
        }

        static Poruka ObradiKlijentZahtev(Poruka zahtev)
        {
            switch (zahtev.Tip)
            {
                case TipPoruke.Prijava:
                    return ProslediServeru(zahtev);
                case TipPoruke.PregledStanja:
                    Poruka stanje = new Poruka(TipPoruke.ProveriStanje)
                    {
                        KorisnikId = zahtev.KorisnikId,
                        FilijalaId = _filijalaId
                    };
                    return ProslediServeru(stanje);
                case TipPoruke.Uplata:
                case TipPoruke.Isplata:
                case TipPoruke.Transfer:
                case TipPoruke.Registracija:
                    return ProslediServeru(zahtev);
                default:
                    return new Poruka(TipPoruke.Greska, "Nepoznat zahtev");
            }
        }

        static Poruka ProslediServeru(Poruka zahtev)
        {
            zahtev.FilijalaId = _filijalaId;
            PosaljiServeru(zahtev);
            return CekajOdgovorServera();
        }

        static void Zatvori()
        {
            Console.WriteLine("\nZatvaranje filijale...");
            _aktivan = false;

            lock (_lock)
            {
                foreach (var klijent in _povezaniKlijenti)
                    klijent.Close();
                _povezaniKlijenti.Clear();
            }

            _tcpListenerSocket?.Close();
            _udpSocket?.Close();
        }
    }
}
