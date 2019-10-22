using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

namespace SixShooter
{
    //TODO
    /// <summary>
    /// Luokka, josta pelin ase luodaan. Sisältää ohjeet aseen piirtämiseksi ruudulle sekä metodit, joilla aseen toimintaa ohjataan.
    /// </summary>
    public class Ase : GameObject
    {

        public delegate void AmpumisDelegaatti();
        public event AmpumisDelegaatti AseellaAmmuttiin;

        public IntMeter Ammukset { get; private set; }
        public DoubleMeter bonusLaskuri { get; private set; }

        private Peli peli;

        public bool OnkoLatausKaynnissa { get; private set; }

        private Timer bonusAjastin;

        private Image[] asePerusKuvat;
        private Image[] aseLatausKuvat;
        private SoundEffect[] aseAanet;




        /// <summary>
        /// Ase-luokan ainut konstruktori. Ottaa parametreinaan pelin alussa ladatut peligrafiikat ja ääniefektit. Kutsuu yläluokan konstruktoria, joka luo oliosta oikean kokoisen ja muotoisen.
        /// </summary>
        /// <param name="peli">Peli, johon ase kuuluuu</param>
        /// <param name="asePeruskuvat">Taulukko, joka sisältää aseen normaalin kuvan sekä laukauksen aukaisen kuvan</param>
        /// <param name="aseLatauskuvat">Taulukko, joka sisältää kuvasarjan aseen lataamisesta</param>
        /// <param name="aseAanet">Taulukko, joka sisältää aseeseen liittyvät äänitehosteet</param>
        public Ase(Peli peli) : base(450, 400)
        {
            this.peli = peli;

            LataaGrafiikkaJaAanet();
            this.Image = asePerusKuvat[0];

            this.Y = -384;
            this.X = 384;
            peli.Add(this, 3);

            //Luodaan ammustilannetta seuraava mittari. Käyttöliittymä kuuntelee suoraan muutoksia mittarin arvoihin.
            Ammukset = new IntMeter(6);
            Ammukset.LowerLimit += LataaAse;

            //Luodaan bonuksen aktivoituessa käynnistyvä ajastin
            bonusAjastin = new Timer();
            bonusLaskuri = new DoubleMeter(0);
            bonusLaskuri.MaxValue = 5;
        }


        /// <summary>
        /// Lataa aseessa käytetyn grafiikan sekä ääänet ja kutsuu Ase-luokan konstruktoria. Mikäli peliin lisätään myöhemmässä vaiheessa muita aseita, siirretään peligrafiikan lataus Ase-luokan sisälle.
        /// </summary>
        private void LataaGrafiikkaJaAanet()
        {
            asePerusKuvat = new Image[2];
            asePerusKuvat[0] = Game.LoadImage("pelaajan_ase");
            asePerusKuvat[1] = Game.LoadImage("pelaajan_ase_laukaus");

            aseLatausKuvat = new Image[7];
            aseLatausKuvat[0] = Game.LoadImage("pelaajan_ase_0");
            aseLatausKuvat[1] = Game.LoadImage("pelaajan_ase_1");
            aseLatausKuvat[2] = Game.LoadImage("pelaajan_ase_2");
            aseLatausKuvat[3] = Game.LoadImage("pelaajan_ase_3");
            aseLatausKuvat[4] = Game.LoadImage("pelaajan_ase_4");
            aseLatausKuvat[5] = Game.LoadImage("pelaajan_ase_5");
            aseLatausKuvat[6] = Game.LoadImage("pelaajan_ase_6");

            aseAanet = new SoundEffect[2];
            aseAanet[0] = Game.LoadSoundEffect("player_gunshot_1");
            aseAanet[1] = Game.LoadSoundEffect("player_reload");
        }
        

        /// <summary>
        /// Metodi vastaa aseen liikuttamista ruudulla hiirtä liikuttaessa. Ase liikkuu vain x-akselin suhteen.
        /// </summary>
        public void LiikutaAsetta()
        {
            double x = peli.Mouse.PositionOnWorld.X;

            //Ase liikkuu vain, kun hiiri on pelialueella
            if (x > 512)
            {
                x = 512;
            }

            if (x < -512)
            {
                x = -512;
            }

            //Aseen liikkuu vain x-akselin suhteen ja vain puolen pelikentän laajuisella alueella.
            this.X = 384 + x / 2;
        }


        /// <summary>
        /// Metodi, jota kutsutaan kun aseella ammutaan. Vastaa aseen ja siihen liittyvien attribuuttien toiminnallisuudesta (kuva- ja ääniefektit, ammuksien kuluminen, jne).
        /// Luo tapahtuman, jota vihollisen tapahtumankäsittelijät kuuntelevat ja käsittelevät mahdollisen osuman.
        /// </summary>
        public void LaukaiseAse()
        {
            //Tarkistetaan ensimmäiseksi onko edellytyksiä ampua aseella.
            if (!peli.OnkoKelloKaynnissa)
            {
                return;
            }
            if (OnkoLatausKaynnissa)
            {
                return;
            }

            //Vaihdetaan laukauksen tapahtuessa hetkeksi toinen kuva
            this.Image = asePerusKuvat[1];
            Timer.SingleShot(0.1, delegate { this.Image = asePerusKuvat[0]; });

            aseAanet[0].Play();
            Console.WriteLine("Bang!");

            //Luodaan tapahtuma, jota vihollisoliot kuuntelevat
            if (AseellaAmmuttiin != null)
            {
                AseellaAmmuttiin();
            };

            //Jos ammusBonus on aktiivinen, laukaus ei kuluta ammuksia. Palataan metodin kutsupisteeseen.
            if (bonusLaskuri.Value > 0)
            {
                return;
            }

            Ammukset.Value -= 1;
        }


        /// <summary>
        /// Vastaa aseen lataamiseen liittyvistä toiminnoista, kuten peligrafiikan ja ammustilannetta seuraavan attribuutin päivittämisestä.
        /// Myöhemmin mahdollisesti lisättävä ominaisuus sallii aseen latauksen keskeyttämisen hiiren vasenta näppäintä painamalla.
        /// </summary>
        public void LataaAse()
        {
            //Tarkistetaan, onko edellinen lataaminen päättynyt
            if (OnkoLatausKaynnissa)
            {
                return;
            }

            OnkoLatausKaynnissa = true;
            Ammukset.Value = 0;

            //Vaihdetaan tähtäimen tilalle toinen kuva ilmaisemaan että ampuminen ei onnistu.
            peli.Kayttoliittyma.TahtainPois();

            //Käynnistetään aseen latausta esittävä animaatio ja lisätään jokaisella ajastimen kierroksella ammuksia käytettäväksi.
            //Lisätään tänne myöhemmin tapahtumankäsittelijä, joka kuuntelee tapahtuuko aseen laukaisuyritys.
            this.Image = aseLatausKuvat[0];
            Timer ajastin = new Timer();
            ajastin.Interval = 0.2;
            ajastin.Timeout += delegate
            {
                this.Image = aseLatausKuvat[Ammukset];
                Ammukset.Value += 1;
                aseAanet[1].Play();
            };
            ajastin.Start(6);

            Console.WriteLine("Ladataan...");

            //Muutetaan latausanimaation päätyttyä ase ampumakelpoiseksi. Toteutusta joudutaan muuttamaan jos aseen laukaiseminen latauksen aikana mahdollistetaan.
            Timer.SingleShot(1.5, delegate
            {
                OnkoLatausKaynnissa = false;
                //Vaihdetaan tähtäin tavalliseksi
                peli.Kayttoliittyma.TahtainPaalle();

                this.Image = asePerusKuvat[0];
                Console.WriteLine("Ase ladattu");
            });

        }


        /// <summary>
        /// Aktivoi vihollisen eliminoimisesta palkinnoksi saadun bonuksen, joka mahdollistaa rajattomat ammukset viiden sekunnin ajaksi
        /// </summary>
        public void AktivoiBonus()
        {
            peli.Kayttoliittyma.NaytaViesti("Rajattomat ammukset 5s", 1);

            //Palautetaan laskuri täyteen
            bonusLaskuri.Value = bonusLaskuri.MaxValue;

            //Pysäytetään mahdollisesti aiemmin käynnistetty ajastin, ja korvataan se uudella oliolla
            bonusAjastin.Stop();
            bonusAjastin = new Timer();

            bonusAjastin.Interval = 1;
            bonusAjastin.Timeout += delegate
            {
                bonusLaskuri.Value = bonusLaskuri.Value - 1;
            };
            bonusAjastin.Start(5);
        }
    }
}