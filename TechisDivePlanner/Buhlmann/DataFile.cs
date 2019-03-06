using System;
using System.Collections.Generic;
using System.Text;

namespace Buhlmann
{
    public class DataFile
    {
        //Input file stored as a string to keep the data with the program for easier debugging. 
        //Could be changed to use a file if desired by simply reading the file into the fileContents string.

        /* Explanation of format for input file:
			Line 1: Description of dive
			Line 2: Number of gas mixes
			Line 2a: FO2, FHe, FN2 for gas mix #1
			Line 2b: FO2, FHe, FN2 for next gas mix (#2)
			Line 2c: FO2, FHe, FN2 for next gas mix (#3)
			Line 2d: FO2, FHe, FN2 for next gas mix (#4)
			Line 3: Oxygen deco factor (usually between 0.8 and 1.0)
			Line 4: Profile code for first dive segment (1 = ascent/descent)
			Line 4a: Start depth, final depth, rate of ascent/descent, gas mix number
			Line 5: Profile code for next dive segment (2 = constant depth)
			Line 5a: Depth, run time at end of segment, gas mix number
			Line 6: Profile code to start decompression sequence (= 99)
			Line 7: Starting depth for decompression sequence
			Line 8: Gas mix number, ascent rate, step size, Hi Gradient Factor, Lo GF
			Line 9: Depth of next change in deco parameters
			Line 10: Gas mix number, ascent rate, step size
			Line 11: Depth of next change in deco parameters
			Line 12: Gas mix number, ascent rate, step size
			Line 13: Depth of next change in deco parameters
			Line 14: Gas mix number, ascent rate, step size
			Line 15: Depth of next change in deco parameters (or zero for surface)
		 */

        string fileContents = @"
			SAMPLE DIVE TO 90 METERS OF SEAWATER GUAGE (MSWG) FOR 20 MINUTES
			4
			.13,.50,.37
			.36,.00,.64
			.50,.00,.50
			.80,.00,.20
			1.0
			1
			0,90,23,1
			2
			90,20,1
			99
			90
			1,-10,3,0.75,0.30
			33
			2,-10,3
			21
			3,-10,3
			9
			4,-10,3
			0";

        string[] tokens;
        int tokenIndex = 1;

        /// <summary>
        /// Constructor
        /// </summary>
        public DataFile()
        {
            //Strip tabs and carriage returns and split into tokens on commas or new lines
            tokens = fileContents.Replace("\t", "").Replace("\r", "").Split(new char[] { ',', '\n' });
            //FIX For . vs , norwegian stuffs ... FIXME should be culture aware
            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = tokens[i].Replace('.', ',');
            }

        }

        /// <summary>
        /// Read next value as a string
        /// </summary>
        /// <returns> String value </returns>
        public string ReadString()
        {
            return tokens[tokenIndex++];
        }

        /// <summary>
        /// Read next value as an integer
        /// </summary>
        /// <returns> Integer value </returns>
        public int ReadInt()
        {
            return Convert.ToInt32(ReadString());
        }

        /// <summary>
        /// Read next value as a double floating point
        /// </summary>
        /// <returns> Double value </returns>
        public double ReadDouble()
        {
            return Convert.ToDouble(ReadString());
        }
    }
}
