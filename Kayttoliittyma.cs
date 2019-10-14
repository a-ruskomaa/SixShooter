using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;


namespace SixShooter
{
    /// <summary>
    /// Luokasta luotu olio vastaa peli-instanssin käyttöliittymäkomponenttien luomisesta ja ylläpidosta.
    /// </summary>
    public class Kayttoliittyma
    {
        private Peli peli;

        private Image[] ammusKuvat;
        private Image[] hitpointKuvat;
        private Image[] bonusKuvat;

        private IntMeter ajastinLaskuri;

        private Label pisteNaytto;
        private Label tasoNaytto;
        private Label ajastinNaytto;
        private Label viestiNaytto;
        private ProgressBar bonusPalkki;

        private Timer tasoAjastin;

        private GameObject ammuksetUI;
        private GameObject hitpointsUI;


        /// <summary>
        /// Konstruktorissa liitetään olio tiettyyn peli-instanssiin ja luodaan tarvittavat käyttöliittymäkomponentit.
        /// </summary>
        /// <param name="peli"></param>
        public Kayttoliittyma(Peli peli)
        {
            this.peli = peli;

            LataaUiGrafiikka();

            //Luodaan komponentti, joka näyttää ammustilanteen ja päivittää sitä tilanteen muuttuessa
            ammuksetUI = new GameObject(ammusKuvat[6]);
            ammuksetUI.Position = new Vector(440, 230);
            peli.Ase.Ammukset.Changed += delegate { ammuksetUI.Image = ammusKuvat[peli.Ase.Ammukset.Value]; };
            peli.Add(ammuksetUI, 3);

            //Luodaan komponentti, joka näyttää osumapisteet ja päivittää sitä tilanteen muuttuessa
            hitpointsUI = new GameObject(hitpointKuvat[3]);
            hitpointsUI.Position = new Vector(Game.Screen.Right - 20 - hitpointsUI.Width / 2, 350);
            peli.Hitpoints.Changed += delegate { hitpointsUI.Image = hitpointKuvat[peli.Hitpoints]; };
            peli.Add(hitpointsUI, 3);

            //Luodaan muut tarvittavat komponentit
            LuoBonusPalkki();
            LuoPisteNaytot();
            LuoTasoNaytto();
            luoAjastinNaytto();
            LuoViestiNaytto();

            //Luodaan tason vaihtumiseen reagoiva tapahtumankäsittelijä
            peli.Taso.Changed += delegate
            {
                tasoAjastin = new Timer();
                tasoAjastin.Interval = 1;
                tasoAjastin.Timeout += delegate
                {
                    ajastinLaskuri.Value = ajastinLaskuri.Value - 1;
                };

                ajastinLaskuri.Value = ajastinLaskuri.MaxValue;

                tasoAjastin.Start(3);
            };
        }


        /// <summary>
        /// Lataa ammus- ja osumapistetilanteen näyttämisessä käytettävät kuvat ja tallentaa viitteet sopiviin taulukoihin
        /// </summary>
        private void LataaUiGrafiikka()
        {
            ammusKuvat = new Image[7];
            ammusKuvat[0] = Game.LoadImage("ammuksia0");
            ammusKuvat[1] = Game.LoadImage("ammuksia1");
            ammusKuvat[2] = Game.LoadImage("ammuksia2");
            ammusKuvat[3] = Game.LoadImage("ammuksia3");
            ammusKuvat[4] = Game.LoadImage("ammuksia4");
            ammusKuvat[5] = Game.LoadImage("ammuksia5");
            ammusKuvat[6] = Game.LoadImage("ammuksia6");

            hitpointKuvat = new Image[6];
            hitpointKuvat[0] = Game.LoadImage("HP0");
            hitpointKuvat[1] = Game.LoadImage("HP1");
            hitpointKuvat[2] = Game.LoadImage("HP2");
            hitpointKuvat[3] = Game.LoadImage("HP3");
            hitpointKuvat[4] = Game.LoadImage("HP4");
            hitpointKuvat[5] = Game.LoadImage("HP5");

            bonusKuvat = new Image[2];
            bonusKuvat[0] = Game.LoadImage("sydan_punainen");
        }


        /// <summary>
        /// Luo käyttöliittymään pistetilannetta seuraavan labelin
        /// </summary>
        private void LuoPisteNaytot()
        {
            pisteNaytto = new Label();
            pisteNaytto.TextColor = Color.Red;
            pisteNaytto.Title = "Pisteet";
            pisteNaytto.X = Game.Screen.Left + 50 + pisteNaytto.Width / 2;
            pisteNaytto.Y = 360;

            //Päivitetään näytön lukemaa pelin pistetilanteen muuttuessa
            pisteNaytto.BindTo(peli.Pisteet);

            //Muutetaan vielä sijaintia siten, että vasen reuna pysyy 50px vasemmasta reunasta vaikka pistelukema kasvaa
            peli.Pisteet.Changed += delegate { pisteNaytto.X = Game.Screen.Left + 50 + pisteNaytto.Width / 2; };

            peli.Add(pisteNaytto);

            //Luodaan label joka näyttää nykyiset ennätyspisteet. Päivittyy vain uuden pelin alussa.
            Label ennatysPisteNaytto = new Label();
            ennatysPisteNaytto.TextColor = Color.Red;
            ennatysPisteNaytto.Text = "Paras tulos: " + peli.EnnatysPisteet.ToString() + " (Taso " + peli.EnnatysTaso.ToString() + ")";
            ennatysPisteNaytto.X = Game.Screen.Left + 50 + ennatysPisteNaytto.Width / 2;
            ennatysPisteNaytto.Y = 330;
            peli.Add(ennatysPisteNaytto);
        }


        /// <summary>
        /// Luo käynnissä olevaa tasoa seuraavan labelin.
        /// </summary>
        private void LuoTasoNaytto()
        {
            tasoNaytto = new Label();
            tasoNaytto.X = 0;
            tasoNaytto.Y = 360;
            tasoNaytto.TextColor = Color.Red;
            tasoNaytto.TextScale = new Vector(1.5, 1.5);

            //Labelin arvo päivittyy automaattisesti tason muuttuessa
            tasoNaytto.BindTo(peli.Taso);

            tasoNaytto.Title = "Taso";

            tasoNaytto.IsVisible = false;

            peli.Add(tasoNaytto);
        }


        /// <summary>
        /// Luo labelin, joka näyttää olion ajastinLaskuri arvon. Käyttöliittymän konstruktorissa luodaan tapahtumankäsittelijä,
        /// joka käynnistää ajastimen automaattisesti tason vaihtuessa.
        /// </summary>
        private void luoAjastinNaytto()
        {
            ajastinLaskuri = new IntMeter(0);
            ajastinLaskuri.MaxValue = 3;

            //Laitetaan ajastin näkymään ja piiloutumaan automaattisesti
            ajastinLaskuri.UpperLimit += delegate
            {
                tasoNaytto.IsVisible = false;
                ajastinNaytto.IsVisible = true;
            };

            ajastinLaskuri.LowerLimit += delegate
            {
                tasoNaytto.IsVisible = true;
                ajastinNaytto.IsVisible = false;
            };

            ajastinNaytto = new Label();
            ajastinNaytto.X = 0;
            ajastinNaytto.Y = 360;
            ajastinNaytto.TextColor = Color.Red;
            ajastinNaytto.TextScale = new Vector(2, 2);

            //Ajastinmen näkymä on sidottu ajastinLaskurin arvoon
            ajastinNaytto.BindTo(ajastinLaskuri);

            ajastinNaytto.IsVisible = false;

            peli.Add(ajastinNaytto);
        }


        /// <summary>
        /// Luo labelin, jonka avulla voidaan näyttää pelaajalle viestejä.
        /// </summary>
        private void LuoViestiNaytto()
        {
            viestiNaytto = new Label();
            viestiNaytto.X = 0;
            viestiNaytto.Y = 300;
            viestiNaytto.TextColor = Color.Red;
            viestiNaytto.TextColor = Color.Red;
            viestiNaytto.TextScale = new Vector(1.5, 1.5);

            viestiNaytto.IsVisible = false;

            peli.Add(viestiNaytto);
        }


        /// <summary>
        /// Luodaan palkki, joka näyttää aseen ammusbonuksen jäljellä olevan ajan. Sidottu kuuntelemaan Ase-luokan bonusmittaria.
        /// Palkki muuttuu näkyväksi bonuksen aktivoituessa ja näkymättömäksi sen loppuessa.
        /// </summary>
        private void LuoBonusPalkki()
        {
            bonusPalkki = new ProgressBar(230, 15);
            bonusPalkki.Position = new Vector(Game.Screen.Right - 30 - bonusPalkki.Width / 2, 300);
            bonusPalkki.IsVisible = false;

            bonusPalkki.BindTo(peli.Ase.bonusLaskuri);
            peli.Add(bonusPalkki);

            peli.Ase.bonusLaskuri.UpperLimit += delegate
            {
                bonusPalkki.IsVisible = true;
            };
            peli.Ase.bonusLaskuri.LowerLimit += delegate
            {
                bonusPalkki.IsVisible = false;
            };
        }


        /// <summary>
        /// Julkinen luokkametodi. Näyttää parametrina saamansa viestin halutun aikaa.
        /// </summary>
        /// <param name="viesti">Viesti joka näytetään</param>
        /// <param name="aika">Viestin näkyvissäoloaika sekunneissa</param>
        public void NaytaViesti(string viesti, int aika)
        {
            viestiNaytto.Text = viesti;
            viestiNaytto.IsVisible = true;

            Timer.SingleShot(aika, delegate
            {
                viestiNaytto.IsVisible = false;
                viestiNaytto.Text = "";
            });
        }


        // Vain pelin kehitystä varten oleva metodi
        public void DebugVaihdaTaso()
        {
            ajastinNaytto.IsVisible = false;
            tasoNaytto.IsVisible = true;
        }

    }

}