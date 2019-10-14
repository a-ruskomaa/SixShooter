using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;


namespace SixShooter
{
    /// <summary>
    /// Luokka, josta luodaan pelin vihollisoliot. Perii luokan GameObject sekä sen metodit sellaisenaan.
    /// </summary>
    public class Vihollinen : GameObject
    {
        private Peli peli;

        public delegate void AmpumisDelegaatti();
        public event AmpumisDelegaatti VihollinenOsui;

        public delegate void OsumisDelegaatti(Vihollinen vihu, Hitbox hitbox);
        public event OsumisDelegaatti PelaajaOsui;

        public int Id { get; private set; }
        public bool OnkoHengissa { get; private set; }
        public bool OnkoPiilossa { get; private set; }

        public enum Hitbox
        {
            Paa,
            Vartalo
        }

        private GameObject suoja;

        private Vector alkupiste;

        private SoundEffect[] laukausAanet;

        private Image[] vihuKuvat;

        private GameObject hitboxVartalo;
        private GameObject hitboxPaa;


        /// <summary>
        /// Luo uuden vihollisolion. Kutsuu yläluokan konstruktoria parametrinaan kutsussa saatu kuva. Tämä luo oikean kokoisen ja muotoisen peliolion, johon muut saadut parametrit liitetään.
        /// </summary>
        /// <param name="id">Peliolion tunnistenumero. Vastaa peliluokan taulukoiden indeksiä, jossa viite olioon sekä sen edessä olevaan esteeseen sijaitsevat.</param>
        /// <param name="sijainti">Vektori(x,y), johon peliolion keskipiste luodaan.</param>
        /// <param name="kerros">Kerros, jolle peliolio on sijoitettu pelikentällä. Käytetään kuvaamaan olion etäisyyttä kamerasta. Olin koko skaalataan parametrin perusteella.</param>
        /// <param name="kuvat">Taulukko, joka sisältää peliolion käyttämän grafiikan. Välitetään yläluokan konstruktorille.</param>
        /// <param name="peli">Peli-instanssi johon objekti on lisätty.</param>
        public Vihollinen(int id, Vector sijainti, int kerros, Image[] kuvat, SoundEffect[] aanet, Peli peli) : base(kuvat[1])
        {
            Id = id;

            this.peli = peli;

            if (kerros == 1)
            {
                Size *= 0.7;
            }
            else if (kerros == -1)
            {
                Size *= 0.55;
            }
            else if (kerros == -3)
            {
                Size *= 0.45;
            }

            //Vihollisen saamat koordinaatit ovat esteen yläreunan tasalla, siirretään alaspäin puolen pituuden verran
            X = sijainti.X;
            Y = sijainti.Y - Height / 2;
            //Y = sijainti.Y;
            //Tallennetaan sijainti myöhempää käyttöä varten
            this.alkupiste = this.Position;

            this.suoja = LuoEste();

            vihuKuvat = kuvat;
            laukausAanet = aanet;

            OnkoHengissa = true;
            OnkoPiilossa = true;

            //Lisätään päälle ja vartalolle hitboxit sopiviin kohtiin. Vain hitboxeihin osuneet laukaukset rekisteröidään.
            hitboxVartalo = new GameObject(0.39 * this.Width, 0.45 * this.Height, Shape.Rectangle);
            Add(hitboxVartalo);

            hitboxPaa = new GameObject(0.27 * this.Width, 0.25 * this.Height, Shape.Circle);
            hitboxPaa.Y += 0.35 * this.Height;
            Add(hitboxPaa);

            //Kuunnellaan pelaajan ampumisen nostamaa tapahtumaa ja käsitellään se asianmukaisesti
            peli.Ase.AseellaAmmuttiin += OsuikoPelaaja;

            //Piilotetaan hitboxit
            DebugPiilotaHitbox();
        }



        /// <summary>
        /// Luodaan pelaajan korkuinen näkymätön este. Pelaajan ampuessa vihollista tätä objektia
        /// hyödynnetään tarkistaessa osuiko pelaaja suojaan, jonka takana vihollinen on.
        /// </summary>
        /// <returns>Palauttaa luodun esteen, jonka konstruktori lisää olion attribuutiksi</returns>
        private GameObject LuoEste()
        {
            GameObject este = new GameObject(this.Width / 2, this.Height, Shape.Rectangle);
            este.Position = this.Position;
            este.Color = Color.Transparent;
            peli.Add(este);
            return este;
        }


        /// <summary>
        /// Metodi, jota peliajastin kutsuu säännöllisin väliajoin. Useista anonyymeista metodeista koostuva
        /// sekvenssi, jolla vihollinen nousee piilosta, ampuu pelaajaa kohti ja piiloutuu uudelleen.
        /// </summary>
        public void Hyokkaa()
        {
            Console.WriteLine(this.Id + " hyokkää");

            //Muutetaan julkista propertya, jotta samaa vihollista ei voida valita uudelleen ennen hyökkäyksen loppumista
            OnkoPiilossa = false;

            Image = vihuKuvat[1];
            double matka = Height / 2;
            double nopeus = Height * 1.5;

            //Vihollinen nopeutuu tasojen vaikeutuessa
            nopeus *= peli.TasoKerroin;

            //Liikutaan pois esteen takaa, tähdätään kun perillä
            MoveTo(new Vector(X, Y + matka), nopeus, delegate
            {
                Console.WriteLine(this.Id + " tähtää");
                Image = vihuKuvat[2];


                //Odotetaan satunnainen aika, jonka jälkeen yritetään ampua. 
                double tahtysAika = RandomGen.NextDouble(1.5, 3.0);
                //Aika lyhenee tasojen vaikeutuessa.
                Timer.SingleShot(tahtysAika / (peli.TasoKerroin * peli.TasoKerroin), delegate
                   {
                       Ammu(tahtysAika);
                   });
            });
        }


        /// <summary>
        /// Metodi vastaa vihollisen ampumisesta. Saa paramentrinaan tähtäämiseen käytetyn ajan, joka vaikuttaa vihollisen osumatarkkuuteen.
        /// Vihollisen osuessa saa aikaan VihollinenOsui-tapahtuman, jota peli-instanssi kuuntelee
        /// </summary>
        /// <param name="tahtaysAika">Tähtäämiseen käytetty aika sekunteina (ilman tason vaikeutuessa nousevan kertoimen vaikutusta)</param>
        private void Ammu(double tahtaysAika)
        {
            //Yritys keskeytetään jos kyseinen vihollinen on kuollut tai peli on päättynyt tähtäämisen aikana
            Console.WriteLine(this.Id + " yrittää ampua");
            if (!OnkoHengissa || !peli.OnkoKelloKaynnissa)
            {
                Console.WriteLine(Id + " ei ampunut; OnkoHengissä: " + OnkoHengissa + " OnkoKelloKäynnissä : " + peli.OnkoKelloKaynnissa);
                return;
            }
            //Ammutaan kohti pelaajaa
            Image = vihuKuvat[3];

            //Arvotaan osuuko vihollinen. Osumatarkkuus paranee tähtäämiseen käytetyn ajan myötä.
            //Parametrina saatu tahtaysAika kertoo ainoastaan tähtäyksen keston arvotun osuuden. Pelin vaikeutuminen ei vaikuta vihollisten osumatarkkuuteen.
            double osumatarkkuus = 1.0;

            if (tahtaysAika < 2.5 && tahtaysAika >= 2.0)
            {
                osumatarkkuus = 0.8;
            }
            if (tahtaysAika < 2)
            {
                osumatarkkuus = 0.5;
            }

            double random = RandomGen.NextDouble(0, 1.0);

            bool osuu = random < osumatarkkuus;

            if (osuu)
            {
                int laukausAani = RandomGen.NextInt(3) + 1;
                laukausAanet[laukausAani].Play();

                //Luo tapahtuman, jota peli-instanssi kuuntelee ja käsittelee tapahtuman
                VihollinenOsui();
                Console.WriteLine(Id + " osui pelaajaan. tahtays: " + tahtaysAika + "tarkkuus: " + osumatarkkuus + " random: " + random);
            } else
            {
                laukausAanet[0].Play();
                Console.WriteLine(Id + " ampui kohti pelaajaa, ei osunut. tahtays: " + tahtaysAika + "tarkkuus: " + osumatarkkuus + " random: " + random);
            }

            Timer.SingleShot(0.2, Piilota);
        }


        /// <summary>
        /// Vastaa vihollisen palauttamisesta esteen taakse hyökkäyksen päätyttyä. Eriytetty koodin selventämiseksi omaan metodiinsa.
        /// </summary>
        private void Piilota()
        {
            Console.WriteLine(this.Id + " piiloutuu");
            Image = vihuKuvat[4];
            double matka = Height / 2;
            double nopeus = Height;

            nopeus *= peli.TasoKerroin;

            MoveTo(new Vector(X, Y - matka), nopeus, delegate
            {
                Image = vihuKuvat[1];
                OnkoPiilossa = true;
            });
        }


        /// <summary>
        /// Metodi, joka tarkistaa pelaajan ampuessa osuiko laukaus tähän olioon. Luo tapahtuman PelaajaOsui pelaajan osuessa.
        /// </summary>
        private void OsuikoPelaaja()
        {
            Console.WriteLine("Tarkistetaan osuiko pelaaja");

            //GameObject suoja = peli.Esteet[Id];
            if (Game.Mouse.IsCursorOn(suoja))
            {
                //Pelaaja osui esteeseen, ei viholliseen
                return;
            }

            //Tarkistetaan mihin osui ja luodaan osumakohdan sisältämä tapahtuma. Peli-luokan tapahtumankäsittelijä kutsuu tällöin luokan sisältämää metodia KasitteleOsumaViholliseen()
            if (Game.Mouse.IsCursorOn(hitboxPaa))
            {
                PelaajaOsui(this, Hitbox.Paa);
                Kuole(Hitbox.Paa);
            }
            else if (Game.Mouse.IsCursorOn(hitboxVartalo))
            {
                PelaajaOsui(this, Hitbox.Vartalo);
                Kuole(Hitbox.Vartalo);
            }
        }


        /// <summary>
        /// Muuttaa peligrafiikkaa asianmukaisesti ja liikuttaa kuolleen vihollisen pois näkyviltä.
        /// </summary>
        /// <param name="mihinOsui">Hitbox, johon pelaaja osui</param>
        private void Kuole(Hitbox mihinOsui)
        {
            //Pysäyttää liikkeen ja estää hyökkäyksen jatkumisen
            StopMoveTo();
            OnkoHengissa = false;

            //Lakataan kuuntelemasta pelaajan ammuskeluita
            peli.Ase.AseellaAmmuttiin -= OsuikoPelaaja;

            //Tarkistaa mihin pelaaja osui. Pääosuman jälkeen vihollinen "antautuu" ja nousee pois kuvaruudulta
            if (mihinOsui == Hitbox.Paa)
            {
                Image = vihuKuvat[5];
                Timer.SingleShot(1, delegate
                {
                    double nopeusYlos = Height * peli.TasoKerroin;
                    MoveTo(new Vector(alkupiste.X, alkupiste.Y + 4 * Height), nopeusYlos * 2);
                });
                return;
            }

            //Vartalo-osuman jälkeen vihollinen valuu alas esteen taakse
            Image = vihuKuvat[6];
            double nopeus = Height / 3;
            MoveTo(alkupiste, nopeus);
        }


        /// <summary>
        /// Palauttaa olion parametrit alkutilaan ja liikuttaa sen alkuperäisiin koordinaatteihin. Peli-instanssi kutsuu metodia jokaisen vihollisen kohdalla kun taso vaihtuu.
        /// </summary>
        public void PalautaHenkiin()
        {
            //Odotellaan sopiva hetki, jonka jälkeen siirretään vihollinen takaisin lähtöpisteeseensä
            Timer.SingleShot(3, delegate
            {
                //Siirretään vihollinen ensin ruudun yläreunaan...
                Image = vihuKuvat[0];
                Position = new Vector(alkupiste.X, alkupiste.Y + 3 * Height);

                //...ja takaisin paikoilleen
                double nopeus = Height;
                MoveTo(alkupiste, nopeus, delegate
                {
                    Image = vihuKuvat[1];
                    OnkoHengissa = true;
                    OnkoPiilossa = true;
                    //Kuunnellaan jälleen pelaajan ammuskeluita
                    peli.Ase.AseellaAmmuttiin += OsuikoPelaaja;
                });
            });
        }


        //Seuraavat metodit ovat pelin kehitystä varten ja poistetaan lopullisessa versiossa
        public void DebugNaytaHitbox()
        {
            this.hitboxVartalo.Color = Color.BloodRed;
            this.hitboxPaa.Color = Color.Red;
            this.suoja.Color = Color.Gray;
        }
        public void DebugPiilotaHitbox()
        {
            this.hitboxVartalo.Color = Color.Transparent;
            this.hitboxPaa.Color = Color.Transparent;
            this.suoja.Color = Color.Transparent;
        }
        public void DebugNaytaUkko()
        {
            Image = vihuKuvat[1];
            double matka = Height / 2;
            double nopeus = Height * 1.5;

            nopeus *= peli.TasoKerroin;

            MoveTo(new Vector(X, Y + matka), nopeus);
        }

    }
}