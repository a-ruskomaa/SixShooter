using System;
using System.Collections.Generic;
using System.Xml;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

namespace SixShooter
{
    //TODO: lisää high score
    //TODO: lisää sijaintien luku ulkoiseen tiedostoon

    /// @author Aleksi Ruskomaa
    /// @version 1.0
    /// 
    /// <summary>
    /// Luokka sisältää pelin toiminnan määrittävät muuttujat ja pelimekaniikkaa ohjaavat keskeiset komponentit.
    /// Peli-instanssin ja käyttöliittymän välinen kommunikointi on pääasiallisesti toteutettu käyttämällä peliluokan vain-luku propertyja.
    /// 
    /// </summary>
    public class Peli : PhysicsGame
    {
        //Propertyt joita kysellään muistakin luokista
        public Kayttoliittyma Kayttoliittyma { get; private set; }
        public Ase Ase { get; private set; }
        public IntMeter Pisteet { get; private set; }
        public int EnnatysPisteet { get; private set; }
        public int EnnatysTaso { get; private set; }
        public IntMeter Hitpoints { get; private set; }
        public IntMeter Taso { get; private set; }
        public double TasoKerroin { get; private set; }
        public bool OnkoKelloKaynnissa { get { return peliAjastin.Enabled; } }

        private Timer peliAjastin;

        private Vector[] sijainnit;

        private SoundEffect tiktak4s;
        private SoundEffect tuomionKello;
        private Image[] osumaOverlayKuvat;

        private List<Vihollinen> viholliset;
        private List<Vihollinen> vihollisetHengissa;

        private bool debugmoodi;
        private bool debugHitboksit;
        private bool debugKuolematon;


        /// <summary>
        /// Alustaa uuden peli-instanssin ja näyttää pelin alkuvalikon.
        /// </summary>
        public override void Begin()
        {
            LataaGraafisetEfektit();

            LataaAaniefektit();

            NaytaAlkuvalikko();
        }


        /// <summary>
        /// Lataa pelissä käytettäviä efektejä.
        /// </summary>
        private void LataaGraafisetEfektit()
        {
            osumaOverlayKuvat = new Image[6];
            osumaOverlayKuvat[0] = LoadImage("osuma00");
            osumaOverlayKuvat[1] = LoadImage("osuma10");
            osumaOverlayKuvat[2] = LoadImage("osuma20");
            osumaOverlayKuvat[3] = LoadImage("osuma30");
            osumaOverlayKuvat[4] = LoadImage("osuma40");
            osumaOverlayKuvat[5] = LoadImage("osuma50");
        }


        /// <summary>
        /// Lataa pelin käyttämät äänitiedostot
        /// </summary>
        private void LataaAaniefektit()
        {
            tiktak4s = LoadSoundEffect("tick_tock_4s");
            tuomionKello = LoadSoundEffect("level_start_bell");
        }


        /// <summary>
        /// Näyttää pelin alkuvalikon. Aktivoituu pelin alussa ja pelin päättyessä.
        /// </summary>
        private void NaytaAlkuvalikko()
        {
            ClearAll(); // Tyhjennetään kenttä kaikista peliolioista

            Level.Background.Image = LoadImage("tausta_title");

            Image aloita_image = LoadImage("aloita");
            Image aloita_mouseover = LoadImage("aloita_mouseover");
            Image lopeta_image = LoadImage("lopeta");
            Image lopeta_mouseover = LoadImage("lopeta_mouseover");

            GameObject title = new GameObject(LoadImage("title"));
            title.Position = new Vector(0, 150);
            Add(title);

            GameObject aloita = new GameObject(aloita_image);
            aloita.Position = new Vector(0, -100);
            Add(aloita);

            GameObject lopeta = new GameObject(lopeta_image);
            lopeta.Position = new Vector(0, -200);
            Add(lopeta);

            Mouse.ListenMovement(1.0, ValikossaLiikkuminen, null);

            //Luodaan valikossa liikkumista varten paikallinen metodi
            void ValikossaLiikkuminen()
            {
                if (Mouse.IsCursorOn(aloita))
                {
                    aloita.Image = aloita_mouseover;
                }
                else
                {
                    aloita.Image = aloita_image;
                }
                if (Mouse.IsCursorOn(lopeta))
                {
                    lopeta.Image = lopeta_mouseover;
                }
                else
                {
                    lopeta.Image = lopeta_image;
                }

            }

            Mouse.ListenOn(aloita, MouseButton.Left, ButtonState.Pressed, AloitaUusiPeli, null);
            Mouse.ListenOn(lopeta, MouseButton.Left, ButtonState.Pressed, Exit, null);
        }


        /// <summary>
        /// Aloittaa uuden pelin. Kutsuu pelin alustavia metodeita, lisää pelioliot ja käynnistää pelimekaniikkaa ohjaavan ajastimen.
        /// </summary>
        private void AloitaUusiPeli()
        {
            ClearAll();

            LataaPelinTausta();

            AlustaPelinMuuttujat();

            LisaaAse();

            LisaaViholliset();

            LuoKuuntelijat();

            Kayttoliittyma = new Kayttoliittyma(this);

            Kayttoliittyma.NaytaViesti("Peli alkaa...", 3);
            tiktak4s.Play();

            Timer.SingleShot(1, delegate
            {
                Taso.Value = 1;
                Timer.SingleShot(3, KaynnistaKello);

            });
        }


        /// <summary>
        /// Lataa kuvatiedostoista pelin taustagrafiikan, piirtää pelimaastoon sijoittuvat esineet kolmeen eri kerrokseen.
        /// </summary>
        private void LataaPelinTausta()
        {
            //Piirretään "tyhjä" taustakuva
            Level.Background.Image = LoadImage("tausta_tyhja");

            //Piirretään taustan muut kerrokset
            GameObject TaustaKerros0 = new GameObject(1024, 340, 0, -214);
            TaustaKerros0.Image = LoadImage("tausta_layer-2");
            Add(TaustaKerros0, -2);

            GameObject TaustaKerros1 = new GameObject(1024, 340, 0, -214);
            TaustaKerros1.Image = LoadImage("tausta_layer0");
            Add(TaustaKerros1, 0);

            GameObject TaustaKerros2 = new GameObject(1024, 340, 0, -214);
            TaustaKerros2.Image = LoadImage("tausta_layer2");
            Add(TaustaKerros2, 2);
        }


        /// <summary>
        /// Alustaa pelin globaalit muuttujat sekä luo peliä ohjaavan ajastimen.
        /// </summary>
        private void AlustaPelinMuuttujat()
        {
            SetWindowSize(1024, 768, false);

            Pisteet = new IntMeter(0);

            //Kokeillaan löytyykö ulkoiseen tiedostoon tallennettuja ennätyspisteitä
            if (DataStorage.Exists("paras_tulos.xml"))
            {
                using (LoadState lataus = DataStorage.BeginLoad("paras_tulos.xml"))
                {
                    EnnatysPisteet = lataus.Load<int>(EnnatysPisteet, "parhaat_pisteet");
                    EnnatysTaso = lataus.Load<int>(EnnatysTaso, "pisteiden_taso");
                }
            }
            else
            {
                EnnatysPisteet = 0;
                EnnatysTaso = 0;
            }
            Console.WriteLine("ennatyspisteet: " + EnnatysPisteet);

            debugmoodi = false;

            //Luodaan mittari seuraamaan pelin meneillään olevaa tasoa. Käyttöliittymä kuuntelee Tason muutoksia.
            Taso = new IntMeter(0);
            TasoKerroin = 1;

            //Luodaan mittari seuraamaan osumapisteitä.
            Hitpoints = new IntMeter(3);

            //Ajastin saa vihollisen hyökkäämään määräajoin. Aika pienenee tasojen noustessa, intervallia säädetään metodilla SeuraavaTaso().
            peliAjastin = new Timer();
            peliAjastin.Interval = 3;
            peliAjastin.Timeout += ArvoSeuraavaAmpuja;

            //Haetaan etukäteen määritellyt esteiden ja vihollisten sijainnit
            sijainnit = PaikkaVektorit();
        }


        /// <summary>
        /// Lataa ulkoisesta tiedostosta koordinaatit, joihin pelin esineet ja viholliset sijoitetaan.
        /// </summary>
        /// <returns>Palauttaa taulukon, jossa on peliin sijoitettavien esineiden koordinaatit vektoreina.</returns>
        private Vector[] PaikkaVektorit()
        {
            XmlDocument sijainnitXML = new XmlDocument();
            sijainnitXML.Load("Content/sijainnit.xml");

            XmlNodeList nodes = sijainnitXML.SelectNodes("/sijainnit/vektori");

            Vector[] sijainnit = new Vector[nodes.Count];

            Console.WriteLine(nodes.Count);

            foreach (XmlNode node in nodes)
            {
                int index = int.Parse(node.InnerText);
                double x = Double.Parse(node.Attributes["x"].Value);
                double y = Double.Parse(node.Attributes["y"].Value);
                sijainnit[index] = new Vector(x, y);
            }

            return sijainnit;
        }


        /// <summary>
        /// Lataa aseessa käytetyn grafiikan sekä ääänet ja kutsuu Ase-luokan konstruktoria. Mikäli peliin lisätään myöhemmässä vaiheessa muita aseita, siirretään peligrafiikan lataus Ase-luokan sisälle.
        /// </summary>
        private void LisaaAse()
        {
            Image[] asePerusKuvat = new Image[2];
            asePerusKuvat[0] = LoadImage("pelaajan_ase");
            asePerusKuvat[1] = LoadImage("pelaajan_ase_laukaus");

            Image[] aseLatausKuvat = new Image[7];
            aseLatausKuvat[0] = LoadImage("pelaajan_ase_0");
            aseLatausKuvat[1] = LoadImage("pelaajan_ase_1");
            aseLatausKuvat[2] = LoadImage("pelaajan_ase_2");
            aseLatausKuvat[3] = LoadImage("pelaajan_ase_3");
            aseLatausKuvat[4] = LoadImage("pelaajan_ase_4");
            aseLatausKuvat[5] = LoadImage("pelaajan_ase_5");
            aseLatausKuvat[6] = LoadImage("pelaajan_ase_6");

            SoundEffect[] aseAanet = new SoundEffect[2];
            aseAanet[0] = LoadSoundEffect("player_gunshot_1");
            aseAanet[1] = LoadSoundEffect("player_reload");

            this.Ase = new Ase(this, asePerusKuvat, aseLatausKuvat, aseAanet);
        }


        /// <summary>
        /// Luo kaikkiin sijainnit[] taulukossa määriteltyihin koordinaatteihin vihollisoliot ja lisää ne peliin.
        /// Kaikki viholliset lisätään myös listalle 'viholliset', josta arvotaan jokaisen tason alussa kuusi vihollista listalle vihollisetHengissa.
        /// </summary>
        private void LisaaViholliset()
        {
            //Ladataan vihollisten peligrafiikka
            Image[] vihuKuvat = new Image[7];
            vihuKuvat[0] = LoadImage("vihu_pose");
            vihuKuvat[1] = LoadImage("vihu_perus");
            vihuKuvat[2] = LoadImage("vihu_tahtaa");
            vihuKuvat[3] = LoadImage("vihu_ampuu");
            vihuKuvat[4] = LoadImage("vihu_ampui");
            vihuKuvat[5] = LoadImage("vihu_headshot");
            vihuKuvat[6] = LoadImage("vihu_kuollut");

            //Ladataan vihollisten peliäänet
            SoundEffect[] vihuAanet = new SoundEffect[4];
            vihuAanet[0] = LoadSoundEffect("enemy_gunshot_miss");
            vihuAanet[1] = LoadSoundEffect("enemy_gunshot_1");
            vihuAanet[2] = LoadSoundEffect("enemy_gunshot_2");
            vihuAanet[3] = LoadSoundEffect("enemy_gunshot_3");

            //Lisätään vihollisia varten tyhjä lista. Listan avulla helpotetaan oikean kokoisten esteiden piirtämistä ja se toimii reservinä, josta uuden tason alkaessa kopioidaan viitteet henkiin herätettyihin vihollisiin.
            viholliset = new List<Vihollinen>();

            //Loopataan sijainnit sisältävän taulukon läpi ja lisätään viholliset sekä esteet vastaaviin kohtiin
            for (int i = 0; i < sijainnit.Length; i++)
            {
                int kerros = 1;

                if (i > 2)
                {
                    kerros = -1;
                }

                if (i > 5)
                {
                    kerros = -3;
                }

                Vihollinen vihu = new Vihollinen(id: i, sijainti: sijainnit[i], kerros, vihuKuvat, vihuAanet, peli: this);

                //Lisätään tapahtumankäsittelijä kuuntelemaan vihollisen Ampui()-tapahtumaa
                vihu.VihollinenOsui += KasitteleOsumaPelaajaan;

                vihu.PelaajaOsui += KasitteleOsumaViholliseen;

                Add(vihu, kerros);
                viholliset.Add(vihu);
            }

            //Arvotaan kaikista vihollisista kuusi ensimmäistä tasoa varten
            vihollisetHengissa = ArvoTasonViholliset(6, viholliset);

            //Pelin kehitysvaiheen attribuutteja
            debugHitboksit = false;
        }


        /// <summary>
        /// Metodi arpoo sattumanvaraiset viholliset parametrina annetulta listalta ja palauttaa ne uudella listalla.
        /// </summary>
        /// <param name="kaikkiVihut">Lista, jolta viholliset arvotaan</param>
        /// <returns>Palauttaa listan, joka sisältää sattumanvaraiset kuusi vihollista</returns>
        private List<Vihollinen> ArvoTasonViholliset(int maara, List<Vihollinen> kaikkiVihut)
        {
            List<Vihollinen> arvotutVihut = new List<Vihollinen>();
            arvotutVihut.AddRange(kaikkiVihut);

            while (arvotutVihut.Count > maara)
            {
                Vihollinen seuraavaVihu = RandomGen.SelectOne<Vihollinen>(arvotutVihut);
                arvotutVihut.Remove(seuraavaVihu);
            }

            return arvotutVihut;
        }


        /// <summary>
        /// Luo tapahtumankäsittelijät pelin ohjaimille.
        /// </summary>
        private void LuoKuuntelijat()
        {
            Mouse.ListenMovement(0.1, Ase.LiikutaAsetta, null);

            Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
            Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

            Mouse.Listen(MouseButton.Left, ButtonState.Pressed, Ase.LaukaiseAse, "Ammu");
            Mouse.Listen(MouseButton.Right, ButtonState.Pressed, Ase.LataaAse, "Lataa");

            Keyboard.Listen(Key.F7, ButtonState.Pressed, DebugKuolemattomuus, "Säätää Debug-moodissa kuolemattomuuden päälle/pois");
            Keyboard.Listen(Key.F8, ButtonState.Pressed, Debug, "Säätää Debug-moodin päälle/pois");
            Keyboard.Listen(Key.F9, ButtonState.Pressed, DebugPaallePois, "Säätää Debug-moodissa pelikellon päälle/pois");
            Keyboard.Listen(Key.F10, ButtonState.Pressed, DebugNaytaHitboksit, "Näyttää debug-moodissa ukkojen ja esteiden hitboksit");
            Keyboard.Listen(Key.F11, ButtonState.Pressed, DebugSeuraavaTaso, "Vaihtaa debug-moodissa seuraavalle tasolle");
        }


        /// <summary>
        /// Vastaa pelimekaniikan käynnistämisestä uuden tason alkaessa.
        /// </summary>
        private void KaynnistaKello()
        {
            tuomionKello.Play();
            peliAjastin.Start();
            //SetCursor(FromTexture2D(tahtainMusta, 25, 25));
            Console.WriteLine("Pelikello käynnistetty");
        }


        /// <summary>
        /// Pysäyttää pelikellon tason loppuessa tai pelaajan kuollessa
        /// </summary>
        private void PysaytaKello()
        {
            peliAjastin.Stop();
            //SetCursor(FromTexture2D(noCanShoot, 25, 25));
            Console.WriteLine("Pelikello pysäytetty");
        }


        /// <summary>
        /// Aktivoituu peliajastimen toimesta määräajoin. Käynnistää vihollisen hyökkäyssekvenssin.
        /// </summary>
        private void ArvoSeuraavaAmpuja()
        {
            //Arvotaan hengissä olevien vihollisten listalta seuraava ampuja
            Vihollinen ampuja = RandomGen.SelectOne<Vihollinen>(vihollisetHengissa);
            Console.WriteLine(ampuja.Id + " valittu");

            //Tarkistetaan onko valittu vihollinen valmiina ampumaan. Tällä hetkellä jättää yhden hyökkäysvuoron väliin. Muokataan kutsua mahdollisesti myöhemmässä versiossa.
            if (!ampuja.OnkoPiilossa)
            {
                Console.WriteLine(ampuja.Id + " ei piilossa, arvotaan seuraava");
                return;
            }

            ampuja.Hyokkaa();
        }


        /// <summary>
        /// Käynnistää pelistä seuraavan tason. Uudet viholliset palautetaan henkiin ja alkupisteisiinsä. Metodi nostaa myös tasokerrointa, joka nopeuttaa vihollisten hyökkäysfrekvenssiä sekä hyökkäyksen kestoa.
        /// </summary>
        private void SeuraavaTaso()
        {
            //Näytetään tason loppumisen ilmaiseva viesti, ajastetaan teksti- ja ääniefektit sekä vihollisten palauttaminen alkupisteisiinsä.
            Kayttoliittyma.NaytaViesti("Viholliset eliminoitu!", 3);

            Timer.SingleShot(2, delegate
            {
                tiktak4s.Play();
            });

            Timer.SingleShot(3, delegate
            {
                Taso.Value += 1;
                TasoKerroin = 1 + Taso / 10.0;
                Pisteet.Value += 500;
            });

            //Arvotaan seuraavan tason kuusi vihollista
            vihollisetHengissa = ArvoTasonViholliset(6, viholliset);

            foreach (var vihu in vihollisetHengissa)
            {
                //Kkutsutaan jokaisen vihollisen kohdalla luokkametodia
                //PalautaHenkiin() joka liikuttaa viholliset takaisin lähtöpisteeseen.
                vihu.PalautaHenkiin();
            };

            //Lisätään pelin vaikeusastetta nopeuttamalla vihollisia 1/n^2
            double aikaSeuraavaanUkkoon = 3 / (TasoKerroin * TasoKerroin);
            peliAjastin.Interval = aikaSeuraavaanUkkoon;

            //Käynnistetään peliajastin kun viholliset ovat ehtineet liikkua alkupisteeseen.
            Timer.SingleShot(6, KaynnistaKello);
        }


        /// <summary>
        /// Tapahtumankäsittelijä aktivoi metodin kun vihollisolion PelaajaOsui-tapahtuma aktivoituu.
        /// Lisää pelaajalle pisteitä ja mahdolisen bonuksen osumakohdan perusteella. Poistaa vihollisen hengissä olevien listalta.
        /// Tarkistaa myös oliko vihollinen tason viimeinen ja käynnistää tällöin seuraavan tason.
        /// </summary>
        /// <param name="vihu">Viite viholliseen, johon on osuttu.</param>
        /// <param name="hitbox">Enum-tyyppinen muuttuja, joka kertoo mihin vihollista on osunut.</param>
        private void KasitteleOsumaViholliseen(Vihollinen vihu, Vihollinen.Hitbox hitbox)
        {
            Console.WriteLine("Käsitellään osuma viholliseen: " + vihu.Id);
            int saadutPisteet = 0;

            if (hitbox == Vihollinen.Hitbox.Paa)
            {
                saadutPisteet = 100;
            }
            else
            {
                saadutPisteet = 50;
            }

            Pisteet.Value += saadutPisteet;
            ArvoBonus(saadutPisteet);

            //Poistetaan vihollinen hengissä olevien listalta ja aloitetaan seuraava taso, jos vihollisia ei enää ole
            vihollisetHengissa.Remove(vihu);
            if (vihollisetHengissa.Count == 0)
            {
                PysaytaKello();
                SeuraavaTaso();
            }
        }


        /// <summary>
        /// Arpoo pelaajalle mahdollisuuden saada bonus vihollisen kuoleman jälkeen. Vaatii refaktoroinnin.
        /// </summary>
        /// <param name="saadutPisteet">Viholliselta saadut pisteet, joista päätellään mihin kohtaan pelaaja osui. Bonuksen todennäköisyys kasvaa pääosuman jälkeen.</param>
        private void ArvoBonus(int saadutPisteet)
        {
            //Pelaaja voi saada pisteitä 50 tai 100, eli kerroin voi olla 1 tai 2. Kerroin on käänteinen, eli korkeampi kerroin pienentää mahdollisuuksia saada bonus.
            //Pääosuman jälkeen yksittäisen bonuksen todennäköisyys on 1:4, vartalo-osuman jälkeen 1:9.
            int kerroin = 100 / saadutPisteet;
            int bonus = RandomGen.NextInt(5 * kerroin);

            if (bonus == 1 && Hitpoints < 5)
            {
                Hitpoints.Value += 1;
                Kayttoliittyma.NaytaViesti("Bonus: +1 HP", 1);
            }

            if (bonus == 2)
            {
                Ase.AktivoiBonus();
            }
        }


        /// <summary>
        /// Vastaa toiminnoista, jotka aktivoituvat vihollisen ampuessa pelaajaa.
        /// Reagoi vihollisolion VihollinenOsui-tapahtumaan.
        /// </summary>
        public void KasitteleOsumaPelaajaan()
        {
            if (debugKuolematon)
            {
                return;
            }

            Hitpoints.Value -= 1;

            //Luodaan efekti, jossa ruutu välähtää hetkellisesti punaisena pelaajan saadessa osuman. Efektin intensiteetti riippuu pelaajan osumapisteiden määrästä.
            GameObject osumaOverlay = new GameObject(1024, 768);
            Add(osumaOverlay, 3);

            if (Hitpoints > 3)
            {
                osumaOverlay.Image = osumaOverlayKuvat[2];
                Timer.SingleShot(0.05, delegate { osumaOverlay.Image = osumaOverlayKuvat[1]; });
                Timer.SingleShot(0.1, delegate { Remove(osumaOverlay); });
                return;
            }

            if (Hitpoints > 0)
            {
                osumaOverlay.Image = osumaOverlayKuvat[3];
                Timer.SingleShot(0.05, delegate { osumaOverlay.Image = osumaOverlayKuvat[2]; });
                Timer.SingleShot(0.1, delegate { osumaOverlay.Image = osumaOverlayKuvat[1]; });
                Timer.SingleShot(0.15, delegate { Remove(osumaOverlay); });
                return;
            }

            //Osumapisteiden loppuessa pysäytetään kello ja päätetään peli.
            if (Hitpoints == 0)
            {
                PysaytaKello();

                osumaOverlay.Image = osumaOverlayKuvat[5];
                Timer.SingleShot(0.2, delegate { osumaOverlay.Image = osumaOverlayKuvat[4]; });
                Timer.SingleShot(0.4, delegate { osumaOverlay.Image = osumaOverlayKuvat[3]; });
                Timer.SingleShot(0.6, delegate { osumaOverlay.Image = osumaOverlayKuvat[2]; });
                Timer.SingleShot(0.8, delegate { osumaOverlay.Image = osumaOverlayKuvat[1]; });

                Console.WriteLine("GAME OVER!");
                Kayttoliittyma.NaytaViesti("Peli ohi!", 5);

                Timer.SingleShot(5, PeliPaattyi);
            }
        }


        /// <summary>
        /// Metodia kutsutaan pelin päättyessä. Tallentaa mahdolliset ennätyspisteet ulkoiseen tiedostoon, palauttaa hiiren kursorin tavalliseksi ja käynnistää alkuvalikon
        /// </summary>
        public void PeliPaattyi()
        {
            if (Pisteet.Value > EnnatysPisteet)
            {
                EnnatysPisteet = Pisteet.Value;
                EnnatysTaso = Taso;
                Console.WriteLine("Ennatyspisteet :" + EnnatysPisteet);
                using (SaveState tallennus = DataStorage.BeginSave("paras_tulos.xml"))
                {
                    tallennus.Save<int>(EnnatysPisteet, "parhaat_pisteet");
                    tallennus.Save<int>(EnnatysTaso, "pisteiden_taso");
                }

            }
            Microsoft.Xna.Framework.Input.Mouse.SetCursor(Microsoft.Xna.Framework.Input.MouseCursor.Arrow);
            NaytaAlkuvalikko();
        }


        //Seuraavat metodit ovat vain pelin kehitystä varten ja poistetaan lopullisesta versiosta
        private void Debug()
        {

            if (!debugmoodi)
            {
                MessageDisplay.TextColor = Color.Red;
                Kayttoliittyma.NaytaViesti("debug päällä", 1);
                debugmoodi = true;
                return;
            }

            if (debugmoodi)
            {
                MessageDisplay.TextColor = Color.Black;
                Kayttoliittyma.NaytaViesti("debug pois", 1);
                debugmoodi = false;
                return;
            }
        }
        private void DebugNaytaHitboksit()
        {
            if (!debugmoodi)
            {
                return;
            }

            if (!debugHitboksit)
            {
                foreach (var ukko in viholliset)
                {
                    ukko.DebugNaytaHitbox();
                }
                debugHitboksit = true;
                return;
            }

            if (debugHitboksit)
            {
                foreach (var ukko in viholliset)
                {
                    ukko.DebugPiilotaHitbox();
                }
                debugHitboksit = false;
                return;
            }
        }
        private void DebugSeuraavaTaso()
        {
            if (!debugmoodi)
            {
                return;
            }

            Taso.Value += 1;
            TasoKerroin = 1 + Taso / 10.0;

            Kayttoliittyma.DebugVaihdaTaso();

            Kayttoliittyma.NaytaViesti("taso + 1", 1);
        }

        private void DebugPaallePois()
        {
            if (!debugmoodi)
            {
                return;
            }

            if (OnkoKelloKaynnissa)
            {
                PysaytaKello();
                Kayttoliittyma.NaytaViesti("kello pysäytetty", 1);
            }
            else
            {
                KaynnistaKello();
                Kayttoliittyma.NaytaViesti("kello käynnistetty", 1);
            }
        }
        
        private void DebugKuolemattomuus()
        {
            if (!debugmoodi)
            {
                return;
            }

            if (debugKuolematon)
            {
                debugKuolematon = false;
                Kayttoliittyma.NaytaViesti("kuolemattomuus pois", 1);
            }
            else
            {
                debugKuolematon = true;
                Kayttoliittyma.NaytaViesti("kuolemattomuus päällä", 1);
            }
        }
    }

}