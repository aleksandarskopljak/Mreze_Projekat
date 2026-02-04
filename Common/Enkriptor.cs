using System;
using System.Text;

namespace Common
    {
        public static class Enkriptor
        {
            public static string Enkriptuj(string poruka, string kljuc)
            {
                if (string.IsNullOrEmpty(poruka) || string.IsNullOrEmpty(kljuc))
                    return poruka;

                var sb = new StringBuilder(poruka.Length);
                int ki = 0; 

                foreach (char ch in poruka)
                {
                    char k = kljuc[ki % kljuc.Length];
                    ki++;

                    if (ch >= 'A' && ch <= 'Z')
                    {
                        int shift = GetLetterShift(k); 
                        sb.Append((char)('A' + ((ch - 'A' + shift) % 26)));
                    }
                    else if (ch >= 'a' && ch <= 'z')
                    {
                        int shift = GetLetterShift(k); 
                        sb.Append((char)('a' + ((ch - 'a' + shift) % 26)));
                    }
                    else if (ch >= '0' && ch <= '9')
                    {
                        int shift = GetDigitShift(k);
                        sb.Append((char)('0' + ((ch - '0' + shift) % 10)));
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }

                return sb.ToString();
            }

            public static string Dekriptuj(string sifrat, string kljuc)
            {
                if (string.IsNullOrEmpty(sifrat) || string.IsNullOrEmpty(kljuc))
                    return sifrat;

                var sb = new StringBuilder(sifrat.Length);
                int ki = 0;

                foreach (char ch in sifrat)
                {
                    char k = kljuc[ki % kljuc.Length];
                    ki++;

                    if (ch >= 'A' && ch <= 'Z')
                    {
                        int shift = GetLetterShift(k);
                        sb.Append((char)('A' + ((ch - 'A' - shift + 26) % 26)));
                    }
                    else if (ch >= 'a' && ch <= 'z')
                    {
                        int shift = GetLetterShift(k);
                        sb.Append((char)('a' + ((ch - 'a' - shift + 26) % 26)));
                    }
                    else if (ch >= '0' && ch <= '9')
                    {
                        int shift = GetDigitShift(k);
                        sb.Append((char)('0' + ((ch - '0' - shift + 10) % 10)));
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }

                return sb.ToString();
            }

            private static int GetLetterShift(char keyChar)
            {
                if (keyChar >= 'A' && keyChar <= 'Z') return keyChar - 'A';
                if (keyChar >= 'a' && keyChar <= 'z') return keyChar - 'a';
                if (keyChar >= '0' && keyChar <= '9') return keyChar - '0';
                return 0;
            }

            private static int GetDigitShift(char keyChar)
            {
                if (keyChar >= '0' && keyChar <= '9') return keyChar - '0';
                if (keyChar >= 'A' && keyChar <= 'Z') return (keyChar - 'A') % 10;
                if (keyChar >= 'a' && keyChar <= 'z') return (keyChar - 'a') % 10;
                return 0;
            }
        }
    }

