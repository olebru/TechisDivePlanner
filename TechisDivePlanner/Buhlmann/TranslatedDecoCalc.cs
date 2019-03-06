using System;

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
        private double WaterVaporPPInLung;    //Water vapor partial pressure in the lungs ph2o -> WaterVaporPPInLung
        private double RunTime;   //Run time rtime -> RunTime
        private int SegmentNumber;     //Segment number segnum -> SegmentNumber
        private double SegmentTime;  //Segment time sgtime -> SegmentTime
        private int MixNumber;     //Mix number mixnum -> MixNumber
        private double AmbientPressure;    //Ambient pressure pamb -> AmbientPressure
        private double GF;  //Gradient factor factor -> GF
        private double O2InDecoGas;  //%O2 in deco gas o2deco -> O2InDecoGas
        //ahe -> AValuesHe

        private double[] AValuesHe = new double[17] { 0, 16.189, 13.830, 11.919, 10.458, 9.220, 8.205, 7.305, 6.502, 5.950, 5.545, 5.333, 5.189, 5.181, 5.176, 5.172, 5.119 };    //Buhlmann A values for H2
                                                                                                                                                                                  // bhe -> BValuesHe
        private double[] BValuesHe = new double[17] { 0, 0.4770, 0.5747, 0.6527, 0.7223, 0.7582, 0.7957, 0.8279, 0.8553, 0.8757, 0.8903, 0.8997, 0.9073, 0.9122, 0.9171, 0.9217, 0.9267 };    //Buhlmann B value for He
                                                                                                                                                                                              // an2 -> AValuesN2
        private double[] AValuesN2 = new double[17] { 0, 11.696, 10.000, 8.618, 7.562, 6.667, 5.600, 4.947, 4.500, 4.187, 3.798, 3.497, 3.223, 2.850, 2.737, 2.523, 2.327 };  //Buhlmann A values for N2
                                                                                                                                                                              // bn2 -> BValuesN2

        private double[] BValuesN2 = new double[17] { 0, 0.5578, 0.6514, 0.7222, 0.7825, 0.8126, 0.8434, 0.8693, 0.8910, 0.9092, 0.9222, 0.9319, 0.9403, 0.9477, 0.9544, 0.9602, 0.9653 };    //Buhlmann B values for N2
                                                                                                                                                                                              // khe -> KValuesHe

        private double[] KValuesHe = new double[17];  //Buhlmann k values for He (log(2)/HalfTime)
                                                      // kn2 -> KValuesN2

        private double[] ppHeInCompartment = new double[17];  //Partial pressure of He in compartment
                                                              // phe -> ppHeInCompartment

        private double[] KValuesN2 = new double[17];  //Buhlmann k values for N2
                                                      // pn2 -> ppN2InCompartment

        private double[] ppN2InCompartment = new double[17];  //Partial pressure of N2 in compartment
                                                              // fhe -> ppHeInMixes

        private double[] ppHeInMixes = new double[11];  //Partial pressure of He in each dive mix
                                                        // fn2 -> ppN2InMixes

        private double[] ppN2InMixes = new double[11];  //Partial pressure of N2 in each dive mix
        private double Ceiling;     //Ceiling
        //pmvmax -> PercentOfMValue
        private double PercentOfMValueMax;      //Percent of M-value
        #endregion

        /// <summary>
        /// Run a deco calculation and display the dive report
        /// </summary>
        public void Run()
        {
            #region Local data
            //fo2 -> pO2InEachMix
            double[] pO2InEachMix = new double[11];  //Partial pressure of O2 in each dive mix (metric system)
            //halfth -> HalfTimesHe
            double[] HalfTimesHe = new double[17] { 0, 1.88, 3.02, 4.72, 6.99, 10.21, 14.48, 20.53, 29.11, 41.21, 55.19, 70.69, 90.34, 115.29, 147.42, 188.24, 240.03 }; //Buhlmann ZH-L16 halftimes for He
            //halftn -> HalfTimesN2                                                                                                                                                                  //halfth -> HalfTimesHe
            double[] HalfTimesN2 = new double[17] { 0, 5.0, 8.0, 12.5, 18.5, 27, 38.3, 54.3, 77.0, 109.0, 146.0, 187.0, 239.0, 305.0, 390.0, 498.0, 635 };   //Buhlmann ZH-L16 halftimes for N2
            int MixNumber; //Current dive mix number nummix -> MixNumber
            double StopDepth;   //Deco stop depth stopd -> StopDepth
            double MeterPrStep;  //Step (in meters) between potential deco stops stepsz -> MeterPrStep
            double DepthOfNextDecoGasMixChange;  //Depth of next deco gas mix change change -> DepthOfNextDecoGasMixChange
            double GFHi;  //High gradient factor fctrhi -> GFHi
            double GFLo;  //Low gradient factor fctrlo -> GFLo
            double GradientFactorSlopeAtLastDecoStop = 0.0;    //Gradient factor slope at last deco stop  // fctrsl -> GradientFactorSlopeAtLastDecoStop
            double DepthOfCurrentDiveSegment;   //Depth of current dive segment depth -> DepthOfCurrentDiveSegment
            double StartingDepth;  //Starting depth  sdepth -> StartingDepth
            double AscendeDescendRateMM;    //Depth change rate (meters/minute) rate -> AscendeDescendRateMM 
            double RuntimeAtTheEndOfDiveSegment;  //Runtime at the end of a dive segment srtime -> RuntimeAtTheEndOfDiveSegment
            double DecoRuntime;  //Deco runtime decort -> DecoRuntime
            double StopTime;   //Stop time stopt -> StopTime
            double temp1;
            double temp2;
            double NextDecoStopDepth;  //Next deco stop depth nxstop -> NextDecoStopDepth
            double StopGF;  //Gradient factor at deco stop depth stopgf StopGF
            double TrialDecoStopDepthPressure;  //Trial deco stop depth pressure triald -> TrialDecoStopDepthPressure
            #endregion

            //Init
            WaterVaporPPInLung = 0.567;
            RunTime = 0.0;
            SegmentNumber = 0;
            for (int i = 1; i <= 16; i++)
            {
                KValuesHe[i] = Math.Log(2.0) / HalfTimesHe[i];
                KValuesN2[i] = Math.Log(2.0) / HalfTimesN2[i];
                ppHeInCompartment[i] = 0.0;
                ppN2InCompartment[i] = 7.452;
            }

            //Load title line and display report heading
            DataFile inFile = new DataFile();   //Like OPEN of input file in FORTRAN
            string line1 = inFile.ReadString();
            Console.WriteLine(format802);
            Console.WriteLine(format800);
            Console.WriteLine(format803, line1);
            Console.WriteLine(format800);

            //Read and display gas mix information
            MixNumber = inFile.ReadInt();
            for (int i = 1; i <= MixNumber; i++)
            {
                pO2InEachMix[i] = inFile.ReadDouble();
                ppHeInMixes[i] = inFile.ReadDouble();
                ppN2InMixes[i] = inFile.ReadDouble();
                double fsum = pO2InEachMix[i] + ppHeInMixes[i] + ppN2InMixes[i];
                double cksum = fsum;
                if (cksum != 1.0)
                {
                    throw new Exception("Error in input file (GASMIX DATA)");
                }
            }
            Console.WriteLine(format810);
            for (int j = 1; j <= MixNumber; j++)
            {
                Console.WriteLine(format811, j, pO2InEachMix[j], ppHeInMixes[j], ppN2InMixes[j]);
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
            Label_ProcessNextDiveProfileSegmentFromFile:
            int MagicNumberSegmentTypeFromFile = inFile.ReadInt();
            if (MagicNumberSegmentTypeFromFile == 1)
            {
                StartingDepth = inFile.ReadDouble();
                double DiveSegmentTargetDepth = inFile.ReadDouble();
                AscendeDescendRateMM = inFile.ReadDouble();
                this.MixNumber = inFile.ReadInt();
                ChangeDepthTo(StartingDepth, DiveSegmentTargetDepth, AscendeDescendRateMM);
                string word;
                if (DiveSegmentTargetDepth > StartingDepth)
                {
                    word = "Descent";
                }
                else if (StartingDepth > DiveSegmentTargetDepth)
                {
                    word = "Ascent";
                }
                else
                {
                    word = "ERROR";
                }
                Console.WriteLine(format830, SegmentNumber, SegmentTime, RunTime, this.MixNumber, word, StartingDepth, DiveSegmentTargetDepth, AscendeDescendRateMM);
            }
            else if (MagicNumberSegmentTypeFromFile == 2)
            {
                DepthOfCurrentDiveSegment = inFile.ReadDouble();
                RuntimeAtTheEndOfDiveSegment = inFile.ReadDouble();
                this.MixNumber = inFile.ReadInt();
                ConstantDepthFor(DepthOfCurrentDiveSegment, RuntimeAtTheEndOfDiveSegment);
                Console.WriteLine(format831, SegmentNumber, SegmentTime, RunTime, this.MixNumber, DepthOfCurrentDiveSegment);
            }
            else if (MagicNumberSegmentTypeFromFile == 99)
            {
                goto Label_StartOfDecoCalculation;
            }
            else
            {
                throw new Exception("ERROR IN INPUT FILE (PROFILE CODE)");
            }
            goto Label_ProcessNextDiveProfileSegmentFromFile;

            //Read first deco mix info and print deco report heading
            Label_StartOfDecoCalculation:
            StartingDepth = inFile.ReadDouble();
            this.MixNumber = inFile.ReadInt();
            AscendeDescendRateMM = inFile.ReadDouble();
            MeterPrStep = inFile.ReadDouble();
            GFHi = inFile.ReadDouble();
            GFLo = inFile.ReadDouble();
            DepthOfNextDecoGasMixChange = inFile.ReadDouble();
            Console.WriteLine(format800);
            Console.WriteLine(format840);
            Console.WriteLine(format800);
            Console.WriteLine(format841);
            Console.WriteLine(format842);
            Console.WriteLine(format843);
            Console.WriteLine(format844);

            //Start the deco ascent processing
            DecoRuntime = 0.0;
            GF = GFLo;
            temp1 = (StartingDepth / 3.0) - 0.5;
            TrialDecoStopDepthPressure = Math.Floor(temp1) * 3.0;
            temp2 = TrialDecoStopDepthPressure;
            if (temp2 <= 0.0)
            {
                TrialDecoStopDepthPressure = 0.0;
            }

            lbl230:
            CalculateCeiling();
            if (Ceiling > TrialDecoStopDepthPressure)
            {
                StopDepth = StartingDepth;
                NextDecoStopDepth = TrialDecoStopDepthPressure;
                goto lbl240;
            }

            //Ascend to next trial deco stop without deco
            ChangeDepthTo(StartingDepth, TrialDecoStopDepthPressure, AscendeDescendRateMM);
            CalculateMaximumMValueForCurrentDiveSegment();
            if (TrialDecoStopDepthPressure == 0.0)
            {
                Console.WriteLine(format850, SegmentNumber, SegmentTime, RunTime, this.MixNumber, TrialDecoStopDepthPressure, AscendeDescendRateMM, PercentOfMValueMax * 100.0, GF);
                goto Label_Finished;
            }
            else
            {
                Console.WriteLine(format851, SegmentNumber, SegmentTime, RunTime, this.MixNumber, TrialDecoStopDepthPressure, AscendeDescendRateMM, PercentOfMValueMax * 100.0);
            }

            //If deco gas needs to change, read in new deco gas info
            if (DepthOfNextDecoGasMixChange == TrialDecoStopDepthPressure)
            {
                this.MixNumber = inFile.ReadInt();
                AscendeDescendRateMM = inFile.ReadDouble();
                MeterPrStep = inFile.ReadDouble();
                DepthOfNextDecoGasMixChange = inFile.ReadDouble();
            }
            StartingDepth = TrialDecoStopDepthPressure;
            TrialDecoStopDepthPressure = StartingDepth - 3.0;
            goto lbl230;

            lbl240:
            if (StopDepth > 0.0)
            {
                GradientFactorSlopeAtLastDecoStop = (GFHi - GFLo) / (0.0 - StopDepth);
            }

            //Process a deco stop
            Label_StartOfDecoStopProcessing:
            StopGF = GF;
            GF = (NextDecoStopDepth * GradientFactorSlopeAtLastDecoStop) + GFHi;
            ProcessDecoStop(StopDepth, NextDecoStopDepth);
            if (DecoRuntime == 0.0)
            {
                StopTime = Math.Round(SegmentTime + 0.5);
            }
            else
            {
                StopTime = RunTime - DecoRuntime;
            }
            Console.WriteLine(format852, SegmentNumber, SegmentTime, RunTime, this.MixNumber, Math.Floor(StopDepth), Math.Floor(StopTime), Math.Floor(RunTime), StopGF);
            StartingDepth = StopDepth;
            StopDepth = NextDecoStopDepth;
            DecoRuntime = RunTime;
            ChangeDepthTo(StartingDepth, StopDepth, AscendeDescendRateMM);
            AmbientPressure = StopDepth + 10.0;
            CalculateMaximumMValueForCurrentDiveSegment();
            if (StopDepth == 0.0)
            {
                Console.WriteLine(format850, SegmentNumber, SegmentTime, RunTime, this.MixNumber, StopDepth, AscendeDescendRateMM, PercentOfMValueMax * 100.0, GF);
            }
            else
            {
                Console.WriteLine(format851, SegmentNumber, SegmentTime, RunTime, this.MixNumber, StopDepth, AscendeDescendRateMM, PercentOfMValueMax * 100.0);
            }
            if (StopDepth == 0.0)
            {
                goto Label_Finished;
            }

            //If deco gas needs to change, read in new deco gas info
            if (DepthOfNextDecoGasMixChange == StopDepth)
            {
                this.MixNumber = inFile.ReadInt();
                AscendeDescendRateMM = inFile.ReadDouble();
                MeterPrStep = inFile.ReadDouble();
                DepthOfNextDecoGasMixChange = inFile.ReadDouble();
            }
            if (StopDepth - MeterPrStep < 0.0)
            {
                NextDecoStopDepth = 0.0;
            }
            else
            {
                NextDecoStopDepth = StopDepth - MeterPrStep;
            }
            goto Label_StartOfDecoStopProcessing;

            Label_Finished:
            return;
        }

        /// <summary>
        /// Ascend or descend
        /// </summary>
        /// <param name="StartingDepth"> starting depth </param>
        /// <param name="FinalDepth"> final depth </param>
        /// <param name="DepthChangeRateMM"> rate, in meters per minute </param>
        private void ChangeDepthTo(double StartingDepth, double FinalDepth, double DepthChangeRateMM)
        {
            double[] pheo = new double[17];
            double[] pn2o = new double[17];

            SegmentTime = (FinalDepth - StartingDepth) / DepthChangeRateMM;
            double TempRunTime = RunTime;
            RunTime = TempRunTime + SegmentTime;
            int TempSegmentNumber = SegmentNumber;
            SegmentNumber = TempSegmentNumber + 1;
            double StartingPressureAmbient = StartingDepth + 10.0;
            double FinalPressureAmbient = FinalDepth + 10.0;
            AmbientPressure = FinalPressureAmbient;
            double piheo = (StartingPressureAmbient - WaterVaporPPInLung) * ppHeInMixes[MixNumber];
            double pin2o = (StartingPressureAmbient - WaterVaporPPInLung) * ppN2InMixes[MixNumber];
            double herate = DepthChangeRateMM * ppHeInMixes[MixNumber];
            double n2rate = DepthChangeRateMM * ppN2InMixes[MixNumber];
            for (int i = 1; i <= 16; i++)
            {
                pheo[i] = ppHeInCompartment[i];
                pn2o[i] = ppN2InCompartment[i];
                ppHeInCompartment[i] = piheo + (herate * (SegmentTime - (1.0 / KValuesHe[i]))) - ((piheo - pheo[i] - (herate / KValuesHe[i])) * Math.Exp(-KValuesHe[i] * SegmentTime));
                ppN2InCompartment[i] = pin2o + (n2rate * (SegmentTime - (1.0 / KValuesN2[i]))) - ((pin2o - pn2o[i] - (n2rate / KValuesN2[i])) * Math.Exp(-KValuesN2[i] * SegmentTime));
            }
        }

        /// <summary>
        /// Process constant-depth dive segment
        /// </summary>
        /// <param name="SegmentDepth"> depth of dive segment </param>
        /// <param name="SegmentTotalAccumulatedRuntime"> segment runtime, in minutes </param>
        private void ConstantDepthFor(double SegmentDepth, double SegmentTotalAccumulatedRuntime)
        {
            double[] pheo = new double[17];
            double[] pn2o = new double[17];
            SegmentTime = SegmentTotalAccumulatedRuntime - RunTime;
            double temprt = SegmentTotalAccumulatedRuntime;
            RunTime = temprt;
            int tempsg = SegmentNumber;
            SegmentNumber = tempsg + 1;
            AmbientPressure = SegmentDepth + 10.0;
            double pihe = (AmbientPressure - WaterVaporPPInLung) * ppHeInMixes[MixNumber];
            double pin2 = (AmbientPressure - WaterVaporPPInLung) * ppN2InMixes[MixNumber];
            for (int i = 1; i <= 16; i++)
            {
                pheo[i] = ppHeInCompartment[i];
                pn2o[i] = ppN2InCompartment[i];
                ppHeInCompartment[i] = pheo[i] + ((pihe - pheo[i]) * (1.0 - Math.Exp(-KValuesHe[i] * SegmentTime)));
                ppN2InCompartment[i] = pn2o[i] + ((pin2 - pn2o[i]) * (1.0 - Math.Exp(-KValuesN2[i] * SegmentTime)));
            }
        }

        /// <summary>
        /// Determine safe ascent ceiling
        /// </summary>
        private void CalculateCeiling()
        {
            double[] ahen2 = new double[17];
            double[] bhen2 = new double[17];
            double[] phen2 = new double[17];
            double[] pambt = new double[17];
            double[] safead = new double[17];

            Ceiling = 0.0;
            for (int i = 1; i <= 16; i++)
            {
                phen2[i] = ppHeInCompartment[i] + ppN2InCompartment[i];
                ahen2[i] = ((ppHeInCompartment[i] * AValuesHe[i]) + (ppN2InCompartment[i] * AValuesN2[i])) / phen2[i];
                bhen2[i] = ((ppHeInCompartment[i] * BValuesHe[i]) + (ppN2InCompartment[i] * BValuesN2[i])) / phen2[i];
                pambt[i] = (phen2[i] - (ahen2[i] * GF)) / ((GF / bhen2[i]) - GF + 1.0);
                safead[i] = pambt[i] - 10.0;
                Ceiling = Math.Max(Ceiling, safead[i]);
            }
        }

        /// <summary>
        /// Process a deco stop
        /// </summary>
        /// <param name="StopDepth"> stop depth </param>
        /// <param name="NextStopDepth"> next deco stop depth </param>
        private void ProcessDecoStop(double StopDepth, double NextStopDepth)
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
            AmbientPressure = StopDepth + 10.0;
            double pihe = (AmbientPressure - WaterVaporPPInLung) * ppHeInMixes[MixNumber];
            if (ppN2InMixes[MixNumber] >= 0.0 && ppN2InMixes[MixNumber] <= 0.2)
            {
                pin2 = (AmbientPressure - WaterVaporPPInLung) * (1.0 - O2InDecoGas + (O2InDecoGas * ppN2InMixes[MixNumber]));
            }
            else
            {
                pin2 = (AmbientPressure - WaterVaporPPInLung) * ppN2InMixes[MixNumber];
            }

            Label_CalculateCompartmentsAndCeiling:
            for (int i = 1; i <= 16; i++)
            {
                pheo[i] = ppHeInCompartment[i];
                pn2o[i] = ppN2InCompartment[i];
                ppHeInCompartment[i] = pheo[i] + ((pihe - pheo[i]) * (1.0 - Math.Exp(-KValuesHe[i] * SegmentTime)));
                ppN2InCompartment[i] = pn2o[i] + ((pin2 - pn2o[i]) * (1.0 - Math.Exp(-KValuesN2[i] * SegmentTime)));
            }

            CalculateCeiling();
            if (Ceiling > NextStopDepth)
            {
                //Ceiling is still below next deco stop. Stay at this depth another minute
                SegmentTime = 1.0;
                count = tempst;
                tempst = count + 1.0;
                temprt = RunTime;
                RunTime = temprt + 1.0;
                goto Label_CalculateCompartmentsAndCeiling;
            }
            SegmentTime = tempst;
        }

        /// <summary>
        /// Calculate the maximum M-value for current dive segment
        /// </summary>
        private void CalculateMaximumMValueForCurrentDiveSegment()
        {
            double[] sumsOfppHeAndPPN2 = new double[17];
            double[] sumsOfAHeAndAN2 = new double[17];
            double[] sumsOfBHeAndAN2 = new double[17];
            double[] MValues = new double[17];
            double[] PercentOfMValueMaxForEachCompartment = new double[17];

            PercentOfMValueMax = 0.0;
            for (int i = 1; i <= 16; i++)
            {
                sumsOfppHeAndPPN2[i] = ppHeInCompartment[i] + ppN2InCompartment[i];
                sumsOfAHeAndAN2[i] = ((ppHeInCompartment[i] * AValuesHe[i]) + (ppN2InCompartment[i] * AValuesN2[i])) / sumsOfppHeAndPPN2[i];
                sumsOfBHeAndAN2[i] = ((ppHeInCompartment[i] * BValuesHe[i]) + (ppN2InCompartment[i] * BValuesN2[i])) / sumsOfppHeAndPPN2[i];
                MValues[i] = (AmbientPressure / sumsOfBHeAndAN2[i]) + sumsOfAHeAndAN2[i];
                PercentOfMValueMaxForEachCompartment[i] = sumsOfppHeAndPPN2[i] / MValues[i];
                PercentOfMValueMax = Math.Max(PercentOfMValueMax, PercentOfMValueMaxForEachCompartment[i]);
            }
        }

        //Simulate FORTRAN format strings
        private const string format800 = " ";
        private const string format801 = "{0,-70}";
        private const string format802 = "                          DECOMPRESSION CALCULATION PROGRAM";
        private const string format803 = "Description:    {0,-70}";
        private const string format810 = "Gasmix Summary:                        FO2    FHe    FN2";
        private const string format811 = "                          Gasmix #{0,2}  {1,5:#.000}  {2,5:#.000}  {3,5:#.000}";
        private const string format812 = "O2 Deco Factor: 80-100% Nitrox or O2 mixes calculated at {0}% of actual O2 fraction";
        private const string format820 = "                                    DIVE PROFILE";
        private const string format821 = "Seq-  Segm.  Run   | Gasmix | Ascent    From     To      Rate    | Constant";
        private const string format822 = "ment  Time   Time  |  Used  |   or     Depth   Depth    +Dn/-Up  |  Depth";
        private const string format823 = "  #   (min)  (min) |    #   | Descent  (mswg)  (mswg)  (msw/min) |  (mswg)";
        private const string format824 = "----- -----  ----- | ------ | -------  ------  ------  --------- | --------";
        private const string format830 = "{0,3}   {1,5:##0.0} {2,6:###0.0} |   {3,2}   | {4,-7}{5,7:######0} {6,7:######0}   {7,7:#####.0}   |";
        private const string format831 = "{0,3}   {1,5:##0.0} {2,6:###0.0} |   {3,2}   |                                    |{4,7:######0}";
        private const string format840 = "                               DECOMPRESSION PROFILE";
        private const string format841 = "Seg-  Segm.  Run   | Gasmix | Ascent   Ascent   Max   |  DECO   STOP   RUN     Gradient";
        private const string format842 = "ment  Time   Time  |  Used  |   To      Rate    %M-   |  STOP   TIME   TIME    Factor";
        private const string format843 = "  #   (min)  (min) |    #   | (mswg) (msw/min) Value  | (mswg)  (min)  (min)    (GF)";
        private const string format844 = "----- -----  ----- | ------ | ------ --------- ------ | ------  -----  -----   ------";
        private const string format850 = "{0,3}   {1,5:##0.0} {2,6:###0.0} |   {3,2}   |  {4,4:###0}   {5,6:###0.0}   {6,5:##0.0}% |                        {7,5:#0.00}";
        private const string format851 = "{0,3}   {1,5:##0.0} {2,6:###0.0} |   {3,2}   |  {4,4:###0}   {5,6:###0.0}   {6,5:##0.0}% |";
        private const string format852 = "{0,3}   {1,5:##0.0} {2,6:###0.0} |   {3,2}   |                         |   {4,3}    {5,3}   {6,4}    {7,5:#0.00}";
    }
}
