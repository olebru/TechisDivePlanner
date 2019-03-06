using System;
using System.Collections.Generic;
using System.Text;

namespace Buhlmann
{
    /// <summary>
    /// Translation of Erik Baker's FORTRAN DecoCalc program into C#
    /// 
    /// For a full explanation of the program and the original source code, see http://www.ddplan.com/reference/decolessons.pdf
    /// 
    ///	This program is essentially a straight "transliteration" of Erik's program. It was intentionally kept
    ///	as close as possible to the original FORTRAN coding as possible. I realize that this makes the program somewhat
    ///	"ugly" and harder to read for people familiar with C#, but I wanted to stay true to the great work that Erik did on the 
    ///	original program and reduce the possibility for introducing bugs.
    ///
    /// Author (translator): Mike Mayfield, mike<at>themayfields.com
    /// 
    /// Public domain. No rights are claimed. This is a derivative work based on Erik Baker's public domain program.
    /// </summary>
    public class TranslatedDecoCalc
    {
        #region Common data
        double WaterVaporPPInLung;    //Water vapor partial pressure in the lungs ph2o -> WaterVaporPPInLung
        double RunTime;   //Run time rtime -> RunTime
        int SegmentNumber;     //Segment number segnum -> SegmentNumber
        double SegmentTime;  //Segment time sgtime -> SegmentTime
        int MixNumber;     //Mix number mixnum -> MixNumber
        double AmbientPressure;    //Ambient pressure pamb -> AmbientPressure
        double GF;  //Gradient factor factor -> GF
        double O2InDecoGas;  //%O2 in deco gas o2deco -> O2InDecoGas
        double[] ahe = new double[17] { 0, 16.189, 13.830, 11.919, 10.458, 9.220, 8.205, 7.305, 6.502, 5.950, 5.545, 5.333, 5.189, 5.181, 5.176, 5.172, 5.119 };    //Buhlmann A values for H2
        double[] bhe = new double[17] { 0, 0.4770, 0.5747, 0.6527, 0.7223, 0.7582, 0.7957, 0.8279, 0.8553, 0.8757, 0.8903, 0.8997, 0.9073, 0.9122, 0.9171, 0.9217, 0.9267 };    //Buhlmann B value for He
        double[] an2 = new double[17] { 0, 11.696, 10.000, 8.618, 7.562, 6.667, 5.600, 4.947, 4.500, 4.187, 3.798, 3.497, 3.223, 2.850, 2.737, 2.523, 2.327 };  //Buhlmann A values for N2
        double[] bn2 = new double[17] { 0, 0.5578, 0.6514, 0.7222, 0.7825, 0.8126, 0.8434, 0.8693, 0.8910, 0.9092, 0.9222, 0.9319, 0.9403, 0.9477, 0.9544, 0.9602, 0.9653 };    //Buhlmann B values for N2
        double[] khe = new double[17];  //Buhlmann k values for He (log(2)/HalfTime)
        double[] kn2 = new double[17];  //Buhlmann k values for N2
        double[] phe = new double[17];  //Partial pressure of He in compartment
        double[] pn2 = new double[17];  //Partial pressure of N2 in compartment
        double[] fhe = new double[11];  //Partial pressure of He in each dive mix
        double[] fn2 = new double[11];  //Partial pressure of N2 in each dive mix
        double ceiling;     //Ceiling
        double pmvmax;      //Percent of M-value
        #endregion

        /// <summary>
        /// Run a deco calculation and display the dive report
        /// </summary>
        public void Run()
        {
            #region Local data
            double[] fo2 = new double[11];  //Partial pressure of O2 in each dive mix (metric system)
            double[] halfth = new double[17] { 0, 1.88, 3.02, 4.72, 6.99, 10.21, 14.48, 20.53, 29.11, 41.21, 55.19, 70.69, 90.34, 115.29, 147.42, 188.24, 240.03 }; //Buhlmann ZH-L16 halftimes for He
            double[] halftn = new double[17] { 0, 5.0, 8.0, 12.5, 18.5, 27, 38.3, 54.3, 77.0, 109.0, 146.0, 187.0, 239.0, 305.0, 390.0, 498.0, 635 };   //Buhlmann ZH-L16 halftimes for N2
            int nummix; //Current dive mix number
            double stopd;   //Deco stop depth
            double stepsz;  //Step (in meters) between potential deco stops
            double change;  //Depth of next deco gas mix change
            double fctrhi;  //High gradient factor
            double fctrlo;  //Low gradient factor
            double fctrsl = 0.0;    //Gradient factor slope at last deco stop
            double depth;   //Depth of current dive segment
            double sdepth;  //Starting depth
            double rate;    //Depth change rate (meters/minute)
            double srtime;  //Runtime at the end of a dive segment
            double decort;  //Deco runtime
            double stopt;   //Stop time
            double temp1;
            double temp2;
            double nxstop;  //Next deco stop depth
            double stopgf;  //Gradient factor at deco stop depth
            double triald;  //Trial deco stop depth pressure
            #endregion

            //Init
            WaterVaporPPInLung = 0.567;
            RunTime = 0.0;
            SegmentNumber = 0;
            for (int i = 1; i <= 16; i++)
            {
                khe[i] = Math.Log(2.0) / halfth[i];
                kn2[i] = Math.Log(2.0) / halftn[i];
                phe[i] = 0.0;
                pn2[i] = 7.452;
            }

            //Load title line and display report heading
            DataFile inFile = new DataFile();   //Like OPEN of input file in FORTRAN
            string line1 = inFile.ReadString();
            Console.WriteLine(format802);
            Console.WriteLine(format800);
            Console.WriteLine(format803, line1);
            Console.WriteLine(format800);

            //Read and display gas mix information
            nummix = inFile.ReadInt();
            for (int i = 1; i <= nummix; i++)
            {
                fo2[i] = inFile.ReadDouble();
                fhe[i] = inFile.ReadDouble();
                fn2[i] = inFile.ReadDouble();
                double fsum = fo2[i] + fhe[i] + fn2[i];
                double cksum = fsum;
                if (cksum != 1.0)
                {
                    throw new Exception("Error in input file (GASMIX DATA)");
                }
            }
            Console.WriteLine(format810);
            for (int j = 1; j <= nummix; j++)
            {
                Console.WriteLine(format811, j, fo2[j], fhe[j], fn2[j]);
            }

            //Read O2 deco info and display dive profile report heading section
            O2InDecoGas = inFile.ReadDouble();
            Console.WriteLine(format800);
            Console.WriteLine(format812, O2InDecoGas * 100.0);
            Console.WriteLine(format800);
            Console.WriteLine(format820);
            Console.WriteLine(format800);
            Console.WriteLine(format821);
            Console.WriteLine(format822);
            Console.WriteLine(format823);
            Console.WriteLine(format824);

            //Read dive profile segment information; dive to that depth for specified duration; display report of dive segments
            lbl100:
            int profil = inFile.ReadInt();
            if (profil == 1)
            {
                sdepth = inFile.ReadDouble();
                double fdepth = inFile.ReadDouble();
                rate = inFile.ReadDouble();
                MixNumber = inFile.ReadInt();
                ASCDEC(sdepth, fdepth, rate);
                string word;
                if (fdepth > sdepth)
                {
                    word = "Descent";
                }
                else if (sdepth > fdepth)
                {
                    word = "Ascent";
                }
                else
                {
                    word = "ERROR";
                }
                Console.WriteLine(format830, SegmentNumber, SegmentTime, RunTime, MixNumber, word, sdepth, fdepth, rate);
            }
            else if (profil == 2)
            {
                depth = inFile.ReadDouble();
                srtime = inFile.ReadDouble();
                MixNumber = inFile.ReadInt();
                CDEPTH(depth, srtime);
                Console.WriteLine(format831, SegmentNumber, SegmentTime, RunTime, MixNumber, depth);
            }
            else if (profil == 99)
            {
                goto lbl200;
            }
            else
            {
                throw new Exception("ERROR IN INPUT FILE (PROFILE CODE)");
            }
            goto lbl100;

            //Read first deco mix info and print deco report heading
            lbl200:
            sdepth = inFile.ReadDouble();
            MixNumber = inFile.ReadInt();
            rate = inFile.ReadDouble();
            stepsz = inFile.ReadDouble();
            fctrhi = inFile.ReadDouble();
            fctrlo = inFile.ReadDouble();
            change = inFile.ReadDouble();
            Console.WriteLine(format800);
            Console.WriteLine(format840);
            Console.WriteLine(format800);
            Console.WriteLine(format841);
            Console.WriteLine(format842);
            Console.WriteLine(format843);
            Console.WriteLine(format844);

            //Start the deco ascent processing
            decort = 0.0;
            GF = fctrlo;
            temp1 = (sdepth / 3.0) - 0.5;
            triald = Math.Floor(temp1) * 3.0;
            temp2 = triald;
            if (temp2 <= 0.0)
            {
                triald = 0.0;
            }

            lbl230:
            SAFASC();
            if (ceiling > triald)
            {
                stopd = sdepth;
                nxstop = triald;
                goto lbl240;
            }

            //Ascend to next trial deco stop without deco
            ASCDEC(sdepth, triald, rate);
            MVCALC();
            if (triald == 0.0)
            {
                Console.WriteLine(format850, SegmentNumber, SegmentTime, RunTime, MixNumber, triald, rate, pmvmax * 100.0, GF);
                goto lbl300;
            }
            else
            {
                Console.WriteLine(format851, SegmentNumber, SegmentTime, RunTime, MixNumber, triald, rate, pmvmax * 100.0);
            }

            //If deco gas needs to change, read in new deco gas info
            if (change == triald)
            {
                MixNumber = inFile.ReadInt();
                rate = inFile.ReadDouble();
                stepsz = inFile.ReadDouble();
                change = inFile.ReadDouble();
            }
            sdepth = triald;
            triald = sdepth - 3.0;
            goto lbl230;

            lbl240:
            if (stopd > 0.0)
            {
                fctrsl = (fctrhi - fctrlo) / (0.0 - stopd);
            }

            //Process a deco stop
            lbl250:
            stopgf = GF;
            GF = (nxstop * fctrsl) + fctrhi;
            DSTOP(stopd, nxstop);
            if (decort == 0.0)
            {
                stopt = Math.Round(SegmentTime + 0.5);
            }
            else
            {
                stopt = RunTime - decort;
            }
            Console.WriteLine(format852, SegmentNumber, SegmentTime, RunTime, MixNumber, Math.Floor(stopd), Math.Floor(stopt), Math.Floor(RunTime), stopgf);
            sdepth = stopd;
            stopd = nxstop;
            decort = RunTime;
            ASCDEC(sdepth, stopd, rate);
            AmbientPressure = stopd + 10.0;
            MVCALC();
            if (stopd == 0.0)
            {
                Console.WriteLine(format850, SegmentNumber, SegmentTime, RunTime, MixNumber, stopd, rate, pmvmax * 100.0, GF);
            }
            else
            {
                Console.WriteLine(format851, SegmentNumber, SegmentTime, RunTime, MixNumber, stopd, rate, pmvmax * 100.0);
            }
            if (stopd == 0.0)
            {
                goto lbl300;
            }

            //If deco gas needs to change, read in new deco gas info
            if (change == stopd)
            {
                MixNumber = inFile.ReadInt();
                rate = inFile.ReadDouble();
                stepsz = inFile.ReadDouble();
                change = inFile.ReadDouble();
            }
            if (stopd - stepsz < 0.0)
            {
                nxstop = 0.0;
            }
            else
            {
                nxstop = stopd - stepsz;
            }
            goto lbl250;

            lbl300:
            return;
        }

        /// <summary>
        /// Ascend or descend
        /// </summary>
        /// <param name="sdepth"> starting depth </param>
        /// <param name="fdepth"> final depth </param>
        /// <param name="rate"> rate, in meters per minute </param>
        void ASCDEC(double sdepth, double fdepth, double rate)
        {
            double[] pheo = new double[17];
            double[] pn2o = new double[17];

            SegmentTime = (fdepth - sdepth) / rate;
            double temprt = RunTime;
            RunTime = temprt + SegmentTime;
            int tempsg = SegmentNumber;
            SegmentNumber = tempsg + 1;
            double spamb = sdepth + 10.0;
            double fpamb = fdepth + 10.0;
            AmbientPressure = fpamb;
            double piheo = (spamb - WaterVaporPPInLung) * fhe[MixNumber];
            double pin2o = (spamb - WaterVaporPPInLung) * fn2[MixNumber];
            double herate = rate * fhe[MixNumber];
            double n2rate = rate * fn2[MixNumber];
            for (int i = 1; i <= 16; i++)
            {
                pheo[i] = phe[i];
                pn2o[i] = pn2[i];
                phe[i] = piheo + (herate * (SegmentTime - (1.0 / khe[i]))) - ((piheo - pheo[i] - (herate / khe[i])) * Math.Exp(-khe[i] * SegmentTime));
                pn2[i] = pin2o + (n2rate * (SegmentTime - (1.0 / kn2[i]))) - ((pin2o - pn2o[i] - (n2rate / kn2[i])) * Math.Exp(-kn2[i] * SegmentTime));
            }
        }

        /// <summary>
        /// Process constant-depth dive segment
        /// </summary>
        /// <param name="depth"> depth of dive segment </param>
        /// <param name="srtime"> segment runtime, in minutes </param>
        void CDEPTH(double depth, double srtime)
        {
            double[] pheo = new double[17];
            double[] pn2o = new double[17];
            SegmentTime = srtime - RunTime;
            double temprt = srtime;
            RunTime = temprt;
            int tempsg = SegmentNumber;
            SegmentNumber = tempsg + 1;
            AmbientPressure = depth + 10.0;
            double pihe = (AmbientPressure - WaterVaporPPInLung) * fhe[MixNumber];
            double pin2 = (AmbientPressure - WaterVaporPPInLung) * fn2[MixNumber];
            for (int i = 1; i <= 16; i++)
            {
                pheo[i] = phe[i];
                pn2o[i] = pn2[i];
                phe[i] = pheo[i] + ((pihe - pheo[i]) * (1.0 - Math.Exp(-khe[i] * SegmentTime)));
                pn2[i] = pn2o[i] + ((pin2 - pn2o[i]) * (1.0 - Math.Exp(-kn2[i] * SegmentTime)));
            }
        }

        /// <summary>
        /// Determine safe ascent ceiling
        /// </summary>
        void SAFASC()
        {
            double[] ahen2 = new double[17];
            double[] bhen2 = new double[17];
            double[] phen2 = new double[17];
            double[] pambt = new double[17];
            double[] safead = new double[17];

            ceiling = 0.0;
            for (int i = 1; i <= 16; i++)
            {
                phen2[i] = phe[i] + pn2[i];
                ahen2[i] = ((phe[i] * ahe[i]) + (pn2[i] * an2[i])) / phen2[i];
                bhen2[i] = ((phe[i] * bhe[i]) + (pn2[i] * bn2[i])) / phen2[i];
                pambt[i] = (phen2[i] - (ahen2[i] * GF)) / ((GF / bhen2[i]) - GF + 1.0);
                safead[i] = pambt[i] - 10.0;
                ceiling = Math.Max(ceiling, safead[i]);
            }
        }

        /// <summary>
        /// Process a deco stop
        /// </summary>
        /// <param name="stopd"> stop depth </param>
        /// <param name="nxstop"> next deco stop depth </param>
        void DSTOP(double stopd, double nxstop)
        {
            double pin2;
            double[] pheo = new double[17];
            double[] pn2o = new double[17];
            double count;

            double temprt = RunTime;
            double round = Math.Round(temprt + 0.5);
            SegmentTime = round - RunTime;
            RunTime = round;
            double tempst = SegmentTime;
            int tempsg = SegmentNumber;
            SegmentNumber = tempsg + 1;
            AmbientPressure = stopd + 10.0;
            double pihe = (AmbientPressure - WaterVaporPPInLung) * fhe[MixNumber];
            if (fn2[MixNumber] >= 0.0 && fn2[MixNumber] <= 0.2)
            {
                pin2 = (AmbientPressure - WaterVaporPPInLung) * (1.0 - O2InDecoGas + (O2InDecoGas * fn2[MixNumber]));
            }
            else
            {
                pin2 = (AmbientPressure - WaterVaporPPInLung) * fn2[MixNumber];
            }

            lbl700:
            for (int i = 1; i <= 16; i++)
            {
                pheo[i] = phe[i];
                pn2o[i] = pn2[i];
                phe[i] = pheo[i] + ((pihe - pheo[i]) * (1.0 - Math.Exp(-khe[i] * SegmentTime)));
                pn2[i] = pn2o[i] + ((pin2 - pn2o[i]) * (1.0 - Math.Exp(-kn2[i] * SegmentTime)));
            }

            SAFASC();
            if (ceiling > nxstop)
            {
                //Ceiling is still below next deco stop. Stay at this depth another minute
                SegmentTime = 1.0;
                count = tempst;
                tempst = count + 1.0;
                temprt = RunTime;
                RunTime = temprt + 1.0;
                goto lbl700;
            }
            SegmentTime = tempst;
        }

        /// <summary>
        /// Calculate the maximum M-value for current dive segment
        /// </summary>
        void MVCALC()
        {
            double[] phen2 = new double[17];
            double[] ahen2 = new double[17];
            double[] bhen2 = new double[17];
            double[] mvalue = new double[17];
            double[] percmv = new double[17];

            pmvmax = 0.0;
            for (int i = 1; i <= 16; i++)
            {
                phen2[i] = phe[i] + pn2[i];
                ahen2[i] = ((phe[i] * ahe[i]) + (pn2[i] * an2[i])) / phen2[i];
                bhen2[i] = ((phe[i] * bhe[i]) + (pn2[i] * bn2[i])) / phen2[i];
                mvalue[i] = (AmbientPressure / bhen2[i]) + ahen2[i];
                percmv[i] = phen2[i] / mvalue[i];
                pmvmax = Math.Max(pmvmax, percmv[i]);
            }
        }

        //Simulate FORTRAN format strings
        const string format800 = " ";
        const string format801 = "{0,-70}";
        const string format802 = "                          DECOMPRESSION CALCULATION PROGRAM";
        const string format803 = "Description:    {0,-70}";
        const string format810 = "Gasmix Summary:                        FO2    FHe    FN2";
        const string format811 = "                          Gasmix #{0,2}  {1,5:#.000}  {2,5:#.000}  {3,5:#.000}";
        const string format812 = "O2 Deco Factor: 80-100% Nitrox or O2 mixes calculated at {0}% of actual O2 fraction";
        const string format820 = "                                    DIVE PROFILE";
        const string format821 = "Seq-  Segm.  Run   | Gasmix | Ascent    From     To      Rate    | Constant";
        const string format822 = "ment  Time   Time  |  Used  |   or     Depth   Depth    +Dn/-Up  |  Depth";
        const string format823 = "  #   (min)  (min) |    #   | Descent  (mswg)  (mswg)  (msw/min) |  (mswg)";
        const string format824 = "----- -----  ----- | ------ | -------  ------  ------  --------- | --------";
        const string format830 = "{0,3}   {1,5:##0.0} {2,6:###0.0} |   {3,2}   | {4,-7}{5,7:######0} {6,7:######0}   {7,7:#####.0}   |";
        const string format831 = "{0,3}   {1,5:##0.0} {2,6:###0.0} |   {3,2}   |                                    |{4,7:######0}";
        const string format840 = "                               DECOMPRESSION PROFILE";
        const string format841 = "Seg-  Segm.  Run   | Gasmix | Ascent   Ascent   Max   |  DECO   STOP   RUN     Gradient";
        const string format842 = "ment  Time   Time  |  Used  |   To      Rate    %M-   |  STOP   TIME   TIME    Factor";
        const string format843 = "  #   (min)  (min) |    #   | (mswg) (msw/min) Value  | (mswg)  (min)  (min)    (GF)";
        const string format844 = "----- -----  ----- | ------ | ------ --------- ------ | ------  -----  -----   ------";
        const string format850 = "{0,3}   {1,5:##0.0} {2,6:###0.0} |   {3,2}   |  {4,4:###0}   {5,6:###0.0}   {6,5:##0.0}% |                        {7,5:#0.00}";
        const string format851 = "{0,3}   {1,5:##0.0} {2,6:###0.0} |   {3,2}   |  {4,4:###0}   {5,6:###0.0}   {6,5:##0.0}% |";
        const string format852 = "{0,3}   {1,5:##0.0} {2,6:###0.0} |   {3,2}   |                         |   {4,3}    {5,3}   {6,4}    {7,5:#0.00}";
    }
}
