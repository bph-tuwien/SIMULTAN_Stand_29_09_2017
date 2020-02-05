using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebServiceConnector.MultizoneService
{
    public class Speicher
    {
        #region parameter
        private String name_id;
        [JsonProperty(PropertyName = "name")]
        public String Name_id
        {
            get { return name_id; }
            set { name_id = value; }
        }

        private double kapazitaet;
        [JsonProperty(PropertyName = "kap")]
        public double Kapazitaet
        {
            get { return kapazitaet; }
            set { kapazitaet = value; }
        }

        private double heizlast;

        [JsonProperty(PropertyName = "heizlastKonstant")]
        public double Heizlast
        {
            get { return heizlast; }
            set { heizlast = value; }
        }

        private double heiztemperatur;

        [JsonProperty(PropertyName = "heizTemp")]
        public double Heiztemperatur
        {
            get { return heiztemperatur; }
            set { heiztemperatur = value; }
        }

        private double tempKonstant;

        [JsonProperty(PropertyName = "tempKonstant")]
        public double TempKonstant
        {
            get { return tempKonstant; }
            set { tempKonstant = value; }
        }

        private String lage;

        [JsonProperty(PropertyName = "lage")]
        public String Lage
        {
            get { return lage; }
            set { lage = value; }
        }

        private int anzahlSchichten;

        [JsonProperty(PropertyName = "anzSchichten")]
        public int AnzahlSchichten
        {
            get { return anzahlSchichten; }
            set { anzahlSchichten = value; }
        }

        private double hoehe;

        [JsonProperty(PropertyName = "hoehe")]
        public double Hoehe
        {
            get { return hoehe; }
            set { hoehe = value; }
        }
        
        private double grundflaeche;

        [JsonProperty(PropertyName = "grundflaeche")]
        public double Grundflaeche
        {
            get { return grundflaeche; }
            set { grundflaeche = value; }
        }

        private double effektiveWaermeleitfaehigkeit;

        [JsonProperty(PropertyName = "leff")]
        public double EffektiveWaermeleitfaehigkeit
        {
            get { return effektiveWaermeleitfaehigkeit; }
            set { effektiveWaermeleitfaehigkeit = value; }
        }
                
        private double vorlaufTemperatur;

        [JsonProperty(PropertyName = "vorlaufTemp")]
        public double VorlaufTemperatur
        {
            get { return vorlaufTemperatur; }
            set { vorlaufTemperatur = value; }
        }

        private double temperaturOben;

        [JsonProperty(PropertyName = "tempeOben")]
        public double TemperaturOben
        {
            get { return temperaturOben; }
            set { temperaturOben = value; }
        }

        private double temperaturUnten;

        [JsonProperty(PropertyName = "tempeUnten")]
        public double TemperaturUnten
        {
            get { return temperaturUnten; }
            set { temperaturUnten = value; }
        }

        private double temperaturKaltwasser;

        [JsonProperty(PropertyName = "tempKaltwasser")]
        public double TemperaturKaltwasser
        {
            get { return temperaturKaltwasser; }
            set { temperaturKaltwasser = value; }
        }

        private double warmwasserbedarf;

        [JsonProperty(PropertyName = "wwb")]
        public double Warmwasserbedarf
        {
            get { return warmwasserbedarf; }
            set { warmwasserbedarf = value; }
        }

        private double fbw;

        [JsonProperty(PropertyName = "fbw")]
        public double Fbw
        {
            get { return fbw; }
            set { fbw = value; }
        }

        private double parameterA;

        [JsonProperty(PropertyName = "a")]
        public double ParameterA
        {
            get { return parameterA; }
            set { parameterA = value; }
        }

        private double parameterB;

        [JsonProperty(PropertyName = "b")]
        public double ParameterB
        {
            get { return parameterB; }
            set { parameterB = value; }
        }

        private double parameterC;

        [JsonProperty(PropertyName = "c")]
        public double ParameterC
        {
            get { return parameterC; }
            set { parameterC = value; }
        }

        private double parameterD;

        [JsonProperty(PropertyName = "d")]
        public double ParameterD
        {
            get { return parameterD; }
            set { parameterD = value; }
        }

        private double parameterBa;

        [JsonProperty(PropertyName = "ba")]
        public double ParameterBa
        {
            get { return parameterBa; }
            set { parameterBa = value; }
        }

        private double parameterZa;

        [JsonProperty(PropertyName = "za")]
        public double ParameterZa
        {
            get { return parameterZa; }
            set { parameterZa = value; }
        }

        private double puffervolumen;

        [JsonProperty(PropertyName = "pufferVollnLiter")]
        public double Puffervolumen
        {
            get { return puffervolumen; }
            set { puffervolumen = value; }
        }

        private double useUaPuffer;
        [JsonProperty(PropertyName = "useUaPuffer")]
        public double UseUaPuffer
        {
            get { return useUaPuffer; }
            set { useUaPuffer = value; }
        }

        private double waermeverluste;

        [JsonProperty(PropertyName = "uaPuffer")]
        public double Waermeverluste
        {
            get { return waermeverluste; }
            set { waermeverluste = value; }
        }

        private double qrho1;

        [JsonProperty(PropertyName = "qrho1")]
        public double Qrho1
        {
            get { return qrho1; }
            set { qrho1 = value; }
        }

        private double l1;

        [JsonProperty(PropertyName = "l1")]
        public double L1
        {
            get { return l1; }
            set { l1 = value; }
        }

        private String z1_id;
        [JsonProperty(PropertyName = "z1")]
        public String Z1_id
        {
            get { return z1_id; }
            set { z1_id = value; }
        }

        private double fero1;

        [JsonProperty(PropertyName = "fero1")]
        public double Fero1
        {
            get { return fero1; }
            set { fero1 = value; }
        }

        private double qrho2;
        [JsonProperty(PropertyName = "qrho2")]
        public double Qrho2
        {
            get { return qrho2; }
            set { qrho2 = value; }
        }

        private double l2;
        [JsonProperty(PropertyName = "l2")]
        public double L2
        {
            get { return l2; }
            set { l2 = value; }
        }

        private String z2_id;
        [JsonProperty(PropertyName = "z2")]
        public String Z2_id
        {
            get { return z2_id; }
            set { z2_id = value; }
        }

        private double fero2;

        [JsonProperty(PropertyName = "fero2")]
        public double Fero2
        {
            get { return fero2; }
            set { fero2 = value; }
        }

        private double qrho3;
        [JsonProperty(PropertyName = "qrho3")]
        public double Qrho3
        {
            get { return qrho3; }
            set { qrho3 = value; }
        }

        private double l3;

        [JsonProperty(PropertyName = "l3")]
        public double L3
        {
            get { return l3; }
            set { l3 = value; }
        }

        private String z3_id;

        public String Z3_id
        {
            get { return z3_id; }
            set { z3_id = value; }
        }

        private double fero3;

        [JsonProperty(PropertyName = "fero3")]
        public double Fero3
        {
            get { return fero3; }
            set { fero3 = value; }
        }

        private double da;

        [JsonProperty(PropertyName = "da")]
        public double Da
        {
            get { return da; }
            set { da = value; }
        }

        private double dr;

        [JsonProperty(PropertyName = "dr")]
        public double Dr
        {
            get { return dr; }
            set { dr = value; }
        }

        private double vf;

        [JsonProperty(PropertyName = "vf")]
        public double Vf
        {
            get { return vf; }
            set { vf = value; }
        }

        private double lf;

        [JsonProperty(PropertyName = "lf")]
        public double Lf
        {
            get { return lf; }
            set { lf = value; }
        }

        private double lb;

        [JsonProperty(PropertyName = "lb")]
        public double Lb
        {
            get { return lb; }
            set { lb = value; }
        }

        private double lr;

        [JsonProperty(PropertyName = "lr")]
        public double Lr
        {
            get { return lr; }
            set { lr = value; }
        }

        private double rhof;

        [JsonProperty(PropertyName = "rhof")]
        public double Rhof
        {
            get { return rhof; }
            set { rhof = value; }
        }

        private double dx;

        [JsonProperty(PropertyName = "dx")]
        public double Dx
        {
            get { return dx; }
            set { dx = value; }
        }

        private double l;

        [JsonProperty(PropertyName = "l")]
        public double L
        {
            get { return l; }
            set { l = value; }
        }

        private double mp;

        [JsonProperty(PropertyName = "mp")]
        public double Mp
        {
            get { return mp; }
            set { mp = value; }
        }

        private double neigung;

        [JsonProperty(PropertyName = "neigung")]
        public double Neigung
        {
            get { return neigung; }
            set { neigung = value; }
        }

        private double ausrichtung;

        [JsonProperty(PropertyName = "ausrichtung")]
        public double Ausrichtung
        {
            get { return ausrichtung; }
            set { ausrichtung = value; }
        }

        private double fRegelung;

        [JsonProperty(PropertyName = "fRegelung")]
        public double FRegelung
        {
            get { return fRegelung; }
            set { fRegelung = value; }
        }

        private double dTSolar;

        [JsonProperty(PropertyName = "dTSolar")]
        public double DTSolar
        {
            get { return dTSolar; }
            set { dTSolar = value; }
        }

        private double fWaermetauscher;

        [JsonProperty(PropertyName = "fWaermetauscher")]
        public double FWaermetauscher
        {
            get { return fWaermetauscher; }
            set { fWaermetauscher = value; }
        }

        private double f0;

        [JsonProperty(PropertyName = "f0")]
        public double F0
        {
            get { return f0; }
            set { f0 = value; }
        }

        private double aKoll;

        [JsonProperty(PropertyName = "aKoll")]
        public double AKoll
        {
            get { return aKoll; }
            set { aKoll = value; }
        }

        private double fs;

        [JsonProperty(PropertyName = "fs")]
        public double Fs
        {
            get { return fs; }
            set { fs = value; }
        }

        private double kd;

        [JsonProperty(PropertyName = "kd")]
        public double Kd
        {
            get { return kd; }
            set { kd = value; }
        }

        private double kb50;

        [JsonProperty(PropertyName = "kb50")]
        public double Kb50
        {
            get { return kb50; }
            set { kb50 = value; }
        }

        private double c1;

        [JsonProperty(PropertyName = "c1")]
        public double C1
        {
            get { return c1; }
            set { c1 = value; }
        }

        private double c2;

        [JsonProperty(PropertyName = "c2")]
        public double C2
        {
            get { return c2; }
            set { c2 = value; }
        }

        private double tempGrenzKollektor;

        [JsonProperty(PropertyName = "tempGrenzKollektor")]
        public double TempGrenzKollektor
        {
            get { return tempGrenzKollektor; }
            set { tempGrenzKollektor = value; }
        }

        private double tempGrenzPuffer;

        [JsonProperty(PropertyName = "tempGrenzPuffer")]
        public double TempGrenzPuffer
        {
            get { return tempGrenzPuffer; }
            set { tempGrenzPuffer = value; }
        }

        private double mpKoll;

        [JsonProperty(PropertyName = "mpKoll")]
        public double MpKoll
        {
            get { return mpKoll; }
            set { mpKoll = value; }
        }

        private double qrho1Koll;

        [JsonProperty(PropertyName = "qrho1Koll")]
        public double Qrho1Koll
        {
            get { return qrho1Koll; }
            set { qrho1Koll = value; }
        }

        private double l1Koll;

        [JsonProperty(PropertyName = "l1Koll")]
        public double L1Koll
        {
            get { return l1Koll; }
            set { l1Koll = value; }
        }

        private double z1Koll;

        [JsonProperty(PropertyName = "z1Koll")]
        public double Z1Koll
        {
            get { return z1Koll; }
            set { z1Koll = value; }
        }

        private double fero1Koll;

        [JsonProperty(PropertyName = "fero1Koll")]
        public double Fero1Koll
        {
            get { return fero1Koll; }
            set { fero1Koll = value; }
        }

        private double qrho2Koll;

        [JsonProperty(PropertyName = "qrho2Koll")]
        public double Qrho2Koll
        {
            get { return qrho2Koll; }
            set { qrho2Koll = value; }
        }

        private double l2Koll;

        [JsonProperty(PropertyName = "l2Koll")]
        public double L2Koll
        {
            get { return l2Koll; }
            set { l2Koll = value; }
        }

        private double z2Koll;

        [JsonProperty(PropertyName = "z2Koll")]
        public double Z2Koll
        {
            get { return z2Koll; }
            set { z2Koll = value; }
        }

        private double fero2Koll;

        [JsonProperty(PropertyName = "fero2Koll")]
        public double Fero2Koll
        {
            get { return fero2Koll; }
            set { fero2Koll = value; }
        }

        private double qrho3Koll;

        [JsonProperty(PropertyName = "qrho3Koll")]
        public double Qrho3Koll
        {
            get { return qrho3Koll; }
            set { qrho3Koll = value; }
        }

        private double l3Koll;

        [JsonProperty(PropertyName = "l3Koll")]
        public double L3Koll
        {
            get { return l3Koll; }
            set { l3Koll = value; }
        }

        private double z3Koll;

        [JsonProperty(PropertyName = "z3Koll")]
        public double Z3Koll
        {
            get { return z3Koll; }
            set { z3Koll = value; }
        }

        private double fero3Koll;

        [JsonProperty(PropertyName = "fero3Koll")]
        public double Fero3Koll
        {
            get { return fero3Koll; }
            set { fero3Koll = value; }
        }

        #endregion

        public Speicher(String name_id, double kapazitaet, double heizlast, double heiztemperatur, double tempKonstant, String lage, int anzahlSchichten, double hoehe, double grundflaeche, double effektiveWaermeleitfaehigkeit, double vorlaufTemperatur, double temperaturOben, double temperaturUnten, double temperaturKaltwasser, double warmwasserbedarf, double fbw, double parameterA, double parameterB, double parameterC, double parameterD, double parameterBa, double parameterZa, double puffervolumen, double useUaPuffer, double waermeverluste, double qrho1, double l1, String z1_id, double fero1, double qrho2, double l2, String z2_id, double fero2, double qrho3, double l3, String z3_id, double fero3, double da, double dr, double vf, double lf, double lb, double lr, double rhof, double dx, double l, double mp, double neigung, double ausrichtung, double fRegelung, double dTSolar, double fWaermetauscher, double f0, double aKoll, double fs, double kd, double kb50, double c1, double c2, double tempGrenzKollektor, double tempGrenzPuffer, double mpKoll, double qrho1Koll, double l1Koll, double z1Koll, double fero1Koll, double qrho2Koll, double l2Koll, double z2Koll, double fero2Koll, double qrho3Koll, double l3Koll, double z3Koll, double fero3Koll)
        {


            
        }
        
    }
}
