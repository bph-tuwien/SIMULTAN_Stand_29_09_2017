using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebServiceConnector.MultizoneService
{
    public class Params
    {
        #region parameter
        
        private double diffTsky;

        [JsonProperty(PropertyName = "diffTsky")]
        public double DiffTsky
        {
            get { return diffTsky; }
            set { diffTsky = value; }
        }

        private double uebergangskoeffizientAußen;

        [JsonProperty(PropertyName = "ae")]
        public double UebergangskoeffizientAußen
        {
            get { return uebergangskoeffizientAußen; }
            set { uebergangskoeffizientAußen = value; }
        }

        private double uebergangskoeffizientBoden;

        [JsonProperty(PropertyName = "aeBoden")]
        public double UebergangskoeffizientBoden
        {
            get { return uebergangskoeffizientBoden; }
            set { uebergangskoeffizientBoden = value; }
        }

        private double uebergangskoeffizientInnen;

        [JsonProperty(PropertyName = "aci")]
        public double UebergangskoeffizientInnen
        {
            get { return uebergangskoeffizientInnen; }
            set { uebergangskoeffizientInnen = value; }
        }

        private double uebergangskoeffizientInnenUp;
        [JsonProperty(PropertyName = "aciUp")]
        public double UebergangskoeffizientInnenUp
        {
            get { return uebergangskoeffizientInnenUp; }
            set { uebergangskoeffizientInnenUp = value; }
        }

        private double uebergangskoeffizientInnenDown;
        [JsonProperty(PropertyName = "aciDown")]
        public double UebergangskoeffizientInnenDown
        {
            get { return uebergangskoeffizientInnenDown; }
            set { uebergangskoeffizientInnenDown = value; }
        }

        private double uebergangskoeffizientInnenStrahlung;
        [JsonProperty(PropertyName = "ari")]
        public double UebergangskoeffizientInnenStrahlung
        {
            get { return uebergangskoeffizientInnenStrahlung; }
            set { uebergangskoeffizientInnenStrahlung = value; }
        }

        private double konvektiverAnteilSolar;
        [JsonProperty(PropertyName = "cfSol")]
        public double KonvektiverAnteilSolar
        {
            get { return konvektiverAnteilSolar; }
            set { konvektiverAnteilSolar = value; }
        }

        private double konvektiverAnteilGeraete;
        [JsonProperty(PropertyName = "cfGer")]
        public double KonvektiverAnteilGeraete
        {
            get { return konvektiverAnteilGeraete; }
            set { konvektiverAnteilGeraete = value; }
        }

        private double konvektiverAnteilPersonen;
        [JsonProperty(PropertyName = "cfPer")]
        public double KonvektiverAnteilPersonen
        {
            get { return konvektiverAnteilPersonen; }
            set { konvektiverAnteilPersonen = value; }
        }

        private double konvektiverAnteilHeizung;
        [JsonProperty(PropertyName = "cfHeat")]
        public double KonvektiverAnteilHeizung
        {
            get { return konvektiverAnteilHeizung; }
            set { konvektiverAnteilHeizung = value; }
        }

        private double temperaturkorrekturfaktor;
        [JsonProperty(PropertyName = "fKorr")]
        public double Temperaturkorrekturfaktor
        {
            get { return temperaturkorrekturfaktor; }
            set { temperaturkorrekturfaktor = value; }
        }

        private double absorptionsgradStrahlung;
        [JsonProperty(PropertyName = "alphaSol")]
        public double AbsorptionsgradStrahlung
        {
            get { return absorptionsgradStrahlung; }
            set { absorptionsgradStrahlung = value; }
        }

        private double waermespeicherkapazitaetWasser;
        [JsonProperty(PropertyName = "cWasser")]
        public double WaermespeicherkapazitaetWasser
        {
            get { return waermespeicherkapazitaetWasser; }
            set { waermespeicherkapazitaetWasser = value; }
        }

        private double waermerueckgewinnungsgrad;
        [JsonProperty(PropertyName = "anlWrg")]
        public double Waermerueckgewinnungsgrad
        {
            get { return waermerueckgewinnungsgrad; }
            set { waermerueckgewinnungsgrad = value; }
        }

        private double bodenreflexionsgrad;
        [JsonProperty(PropertyName = "rho")]
        public double Bodenreflexionsgrad
        {
            get { return bodenreflexionsgrad; }
            set { bodenreflexionsgrad = value; }
        }

        private double temperaturdifferenz;
        [JsonProperty(PropertyName = "tempGap")]
        public double Temperaturdifferenz
        {
            get { return temperaturdifferenz; }
            set { temperaturdifferenz = value; }
        }

        private double anzahlStunden;
        [JsonProperty(PropertyName = "anzStunden")]
        public double AnzahlStunden
        {
            get { return anzahlStunden; }
            set { anzahlStunden = value; }
        }
        /*
        private double ausgabeintervall;
        [JsonProperty(PropertyName = "interval")]
        public double Ausgabeintervall
        {
            get { return ausgabeintervall; }
            set { ausgabeintervall = value; }
        }

        private double status;
        [JsonProperty(PropertyName = "status")]
        public double Status
        {
            get { return status; }
            set { status= value; }
        }

        private double fortschritt;
        [JsonProperty(PropertyName = "remaining")]
        public double Fortschritt
        {
            get { return fortschritt; }
            set { fortschritt = value; }
        }

        private bool klimafile;
        [JsonProperty(PropertyName = "tmy2")]
        public bool Klimafile
        {
            get { return klimafile; }
            set { klimafile = value; }
        }

        private bool fensterlueftung;
        [JsonProperty(PropertyName = "forceLueftung")]
        public bool Fensterlueftung
        {
            get { return fensterlueftung; }
            set { fensterlueftung = value; }
        }

        private bool sommerLueftung;
        [JsonProperty(PropertyName = "sommerLueftung")]
        public bool SommerLueftung
        {
            get { return sommerLueftung; }
            set { sommerLueftung = value; }
        }

        private double genauigkeit;
        [JsonProperty(PropertyName = "precision")]
        public double Genauigkeit
        {
            get { return genauigkeit; }
            set { genauigkeit = value; }
        }

        private double schrittweite;
        [JsonProperty(PropertyName = "maxStep")]
        public double Schrittweite
        {
            get { return schrittweite; }
            set { schrittweite = value; }
        }

        private String solver;
        [JsonProperty(PropertyName = "solver")]
        public String Solver
        {
            get { return solver; }
            set { solver = value; }
        }*/
        #endregion

        //, double ausgabeintervall, double status, double fortschritt, bool klimafile, bool fensterlueftung, bool sommerLueftung, double genauigkeit, double schrittweite, String solver=null
        public Params(double diffTsky=10, double uebergangskoeffizientAußen=25, double uebergangskoeffizientBoden=10000, double uebergangskoeffizientInnen=2.5, double uebergangskoeffizientInnenUp=5, double uebergangskoeffizientInnenDown=0.7, double uebergangskoeffizientInnenStrahlung=5, double konvektiverAnteilSolar=0.1, double konvektiverAnteilGeraete=0.8, double konvektiverAnteilPersonen=0.5, double konvektiverAnteilHeizung=1, double temperaturkorrekturfaktor=0.7, double absorptionsgradStrahlung=0.5, double waermespeicherkapazitaetWasser=4182, double waermerueckgewinnungsgrad=0.8, double bodenreflexionsgrad=0.25, double temperaturdifferenz=1, double anzahlStunden=8760)
        {
            DiffTsky= diffTsky;
            UebergangskoeffizientAußen= uebergangskoeffizientAußen;
            UebergangskoeffizientBoden=  uebergangskoeffizientBoden;
            UebergangskoeffizientInnen= uebergangskoeffizientInnen;
            UebergangskoeffizientInnenUp= uebergangskoeffizientInnenUp;
            UebergangskoeffizientInnenDown= uebergangskoeffizientInnenDown;
            UebergangskoeffizientInnenStrahlung= uebergangskoeffizientInnenStrahlung;
            KonvektiverAnteilSolar= konvektiverAnteilSolar;
            KonvektiverAnteilGeraete = konvektiverAnteilGeraete;
            KonvektiverAnteilPersonen=konvektiverAnteilPersonen;
            KonvektiverAnteilHeizung=konvektiverAnteilHeizung;
            Temperaturkorrekturfaktor=temperaturkorrekturfaktor;
            AbsorptionsgradStrahlung=absorptionsgradStrahlung;
            WaermespeicherkapazitaetWasser=waermespeicherkapazitaetWasser;
            Waermerueckgewinnungsgrad=waermerueckgewinnungsgrad;
            Bodenreflexionsgrad=bodenreflexionsgrad;
            Temperaturdifferenz=temperaturdifferenz;
            AnzahlStunden=anzahlStunden;
            /*Ausgabeintervall=ausgabeintervall;
            Status=status;
            Fortschritt=fortschritt;
            Klimafile=klimafile;
            Fensterlueftung=fensterlueftung;
            SommerLueftung=sommerLueftung;
            Genauigkeit=genauigkeit;
            Schrittweite=schrittweite;
            Solver=solver;
             */
        }

       /* public Params(Params par)
        {
            DiffTsky = par.DiffTsky;
            UebergangskoeffizientAußen = par.UebergangskoeffizientAußen;
            UebergangskoeffizientBoden = par.UebergangskoeffizientBoden;
            UebergangskoeffizientInnen = par.UebergangskoeffizientInnen;
            UebergangskoeffizientInnenUp = par.UebergangskoeffizientInnenUp;
            UebergangskoeffizientInnenDown = par.UebergangskoeffizientInnenDown;
            UebergangskoeffizientInnenStrahlung = par.UebergangskoeffizientInnenStrahlung;
            KonvektiverAnteilSolar = par.KonvektiverAnteilSolar;
            KonvektiverAnteilGeraete = par.KonvektiverAnteilGeraete;
            KonvektiverAnteilPersonen = par.KonvektiverAnteilPersonen;
            KonvektiverAnteilHeizung = par.KonvektiverAnteilHeizung;
            Temperaturkorrekturfaktor = par.Temperaturkorrekturfaktor;
            AbsorptionsgradStrahlung = par.AbsorptionsgradStrahlung;
            WaermespeicherkapazitaetWasser = par.WaermespeicherkapazitaetWasser;
            Waermerueckgewinnungsgrad = par.Waermerueckgewinnungsgrad;
            Bodenreflexionsgrad = par.Bodenreflexionsgrad;
            Temperaturdifferenz = par.Temperaturdifferenz;
            AnzahlStunden = par.AnzahlStunden;
            Ausgabeintervall = par.Ausgabeintervall;
            Status = par.Status;
            Fortschritt = par.Fortschritt;
            Klimafile = par.Klimafile;
            Fensterlueftung = par.Fensterlueftung;
            SommerLueftung = par.SommerLueftung;
            Genauigkeit = par.Genauigkeit;
            Schrittweite = par.Schrittweite;
            Solver = par.Solver;
        }
        */

    }
}
