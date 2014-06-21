using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using EMU7800.Core;

/*
 * unlike EMU7800 Core stuff, this has been hacked around significantly
 */

namespace EMU7800.Win
{
    public class GameProgramLibrary : Dictionary<string, GameProgram>
    {
        #region Fields
		
        const string
            BIOS78_NTSC_MD5             = "0763f1ffb006ddbe32e52d497ee848ae",
            BIOS78_NTSC_ALTERNATE_MD5   = "b32526ea179dc9ab9b2e5f8a2662b298",
            BIOS78_PAL_MD5              = "397bb566584be7b9764e7a68974c4263",
            HSC78_MD5                   = "c8a73288ab97226c52602204ab894286";
		/*
        readonly IDictionary<string, string> _misc7800DiscoveredRoms = new Dictionary<string, string>();
		*/
        // these enums are used for matching column names in the .csv file
        enum Column
        {
            None,
            MD5,
            Title,
            Manufacturer,
            Author,
            Year,
            ModelNo,
            Rarity,
            CartType,
            MachineType,
            LController,
            RController,
            HelpUri
        };

        //const string RomPropertiesFileName = "ROMProperties.csv";

        static readonly Dictionary<Column, int> _columnPositions = new Dictionary<Column, int>
        {
            { Column.MD5, -1},
            { Column.Title, -1},
            { Column.Manufacturer, -1},
            { Column.Author, -1 },
            { Column.Year, - 1},
            { Column.ModelNo, -1},
            { Column.Rarity, -1},
            { Column.CartType, -1},
            { Column.MachineType, -1},
            { Column.LController, -1},
            { Column.RController, -1},
            { Column.HelpUri, -1},
        };

        //readonly RomFileAccessor _romFileAccessor = new RomFileAccessor();
        readonly MD5CryptoServiceProvider _cryptoProvider = new MD5CryptoServiceProvider();
        readonly StringBuilder _sb = new StringBuilder();
        //readonly ILogger _logger;

        #endregion

		public static GameProgramLibrary EMU7800DB = null;

        #region Constructors

        private GameProgramLibrary()
        {
        }

        public GameProgramLibrary(TextReader r)//, ILogger logger)
        {
            //if (logger == null)
            //    throw new ArgumentNullException("logger");

            //_logger = logger;

            //var settings = new GlobalSettings(logger);
            //var fn = Path.Combine(settings.BaseDirectory, RomPropertiesFileName);

            Clear();
            //StreamReader r = null;
            try
            {
                //r = new StreamReader(fn);
                InitializeColumnPos(r.ReadLine());

                while (true)
                {
                    var line = r.ReadLine();
                    if (line == null)
                        break;
                    var gp = CreateGameSettingsFromLine(line);
                    if (gp == null)
                        continue;
                    if (ContainsKey(gp.MD5))
                        Console.WriteLine("7800DB: Duplicate MD5 key found: {0}", gp.MD5); else Add(gp.MD5, gp);
                }
                r.Close();
            }
            catch (Exception ex)
            {
                //if (Util.IsCriticalException(ex))
                    throw;
                //_logger.WriteLine(ex);
            }
            finally
            {
                if (r != null)
                    r.Dispose();
            }

            Console.WriteLine("7800DB: {0} entries loaded.", Count);
        }

        #endregion

        #region Game Program Accessors

		public GameProgram TryRecognizeRom(byte[] bytes)
		{
			//if (string.IsNullOrWhiteSpace(fullName))
			//    throw new ArgumentException("fullName");

			//var bytes = _romFileAccessor.GetRomBytes(fullName);
			if (bytes == null)
				return null;

			var md5 = ComputeMD5Digest(bytes);
			if (string.IsNullOrWhiteSpace(md5))
				return null;

			var gp = GetGameProgramFromMd5(md5);
			if (gp == null)
				gp = GameProgram.GetCompleteGuess(md5);
			//gp.DiscoveredRomFullName = fullName;
			if (gp.CartType == CartType.None)
			{
				switch (gp.MachineType)
				{
					case MachineType.A2600NTSC:
					case MachineType.A2600PAL:
						switch (bytes.Length)
						{
							case 2048: gp.CartType = CartType.A2K; break;
							case 4096: gp.CartType = CartType.A4K; break;
							case 8192: gp.CartType = CartType.A8K; break;
							case 16384: gp.CartType = CartType.A16K; break;
						}
						break;
					case MachineType.A7800NTSC:
					case MachineType.A7800PAL:
						switch (bytes.Length)
						{
							case 8192: gp.CartType = CartType.A7808; break;
							case 16384: gp.CartType = CartType.A7816; break;
							case 32768: gp.CartType = CartType.A7832; break;
							case 49152: gp.CartType = CartType.A7848; break;
						}
						break;
				}
			}
			return gp;
			/*
            if (md5.Equals(HSC78_MD5, StringComparison.OrdinalIgnoreCase))
            {
                if (!_misc7800DiscoveredRoms.ContainsKey(md5))
                    _misc7800DiscoveredRoms.Add(md5, fullName);
                _logger.WriteLine("Found 7800 Highscore Cart: {0}", fullName);
                return null;
            }
            if (md5.Equals(BIOS78_NTSC_MD5, StringComparison.OrdinalIgnoreCase))
            {
                if (!_misc7800DiscoveredRoms.ContainsKey(md5))
                    _misc7800DiscoveredRoms.Add(md5, fullName);
                _logger.WriteLine("Found 7800 NTSC BIOS: {0}", fullName);
                return null;
            }
            if (md5.Equals(BIOS78_NTSC_ALTERNATE_MD5, StringComparison.OrdinalIgnoreCase))
            {
                if (!_misc7800DiscoveredRoms.ContainsKey(md5))
                    _misc7800DiscoveredRoms.Add(md5, fullName);
                _logger.WriteLine("Found incorrect but widely used 7800 NTSC BIOS: {0}", fullName);
                return null;
            }
            if (md5.Equals(BIOS78_PAL_MD5, StringComparison.OrdinalIgnoreCase))
            {
                if (!_misc7800DiscoveredRoms.ContainsKey(md5))
                    _misc7800DiscoveredRoms.Add(md5, fullName);
                _logger.WriteLine("Found 7800 PAL BIOS: {0}", fullName);
                return null;
            }
			*/
		}

		/*
        public GameProgram GetGameProgramFromFullName(string fullName)
        {
            var bytes = _romFileAccessor.GetRomBytes(fullName);
            if (bytes == null)
                throw new ArgumentException("File not readable: {0}", fullName);
            var md5 = ComputeMD5Digest(bytes);
            return !string.IsNullOrWhiteSpace(md5) ? GetGameProgramFromMd5(md5) : null;
        }
		*/
        public GameProgram GetGameProgramFromMd5(string md5)
        {
            if (string.IsNullOrWhiteSpace(md5))
                throw new ArgumentNullException("md5");
            GameProgram gp;
            return TryGetValue(md5, out gp) ? gp : null;
        }
		/*
        public byte[] GetRomBytes(string fullName)
        {
            return _romFileAccessor.GetRomBytes(fullName);
        }

        public byte[] Get78HighScoreCartBytes()
        {
            string fullName;
            if (!_misc7800DiscoveredRoms.TryGetValue(HSC78_MD5, out fullName))
                return null;
            return _romFileAccessor.GetRomBytes(fullName);
        }

        public byte[] Get78BiosBytes(MachineType machineType)
        {
            string fullName = null;
            switch (machineType)
            {
                case MachineType.A7800NTSC:
                    if (!_misc7800DiscoveredRoms.TryGetValue(BIOS78_NTSC_MD5, out fullName))
                        _misc7800DiscoveredRoms.TryGetValue(BIOS78_NTSC_ALTERNATE_MD5, out fullName);
                    break;
                case MachineType.A7800PAL:
                    _misc7800DiscoveredRoms.TryGetValue(BIOS78_PAL_MD5, out fullName);
                    break;
            }
            if (string.IsNullOrWhiteSpace(fullName))
                return null;
            return _romFileAccessor.GetRomBytes(fullName);
        }*/

        #endregion

        #region Game Progam Related Utilities
		/*
        public string ComputeMD5Digest(string fullName)
        {
            var bytes = _romFileAccessor.GetRomBytes(fullName);
            if (bytes == null)
                throw new ArgumentException("File not readable: {0}", fullName);
            return ComputeMD5Digest(bytes);
        }
		*/
        #endregion

        #region Helpers

        static void InitializeColumnPos(string line)
        {
            var colno = 0;
            var columnNames = line.Split(',');

            foreach (var columnName in columnNames)
            {
                var col = ToColumn(columnName);
                if (col != Column.None)
                    _columnPositions[col] = colno;
                colno++;
            }

            if (_columnPositions[Column.MD5] < 0)
                throw new ApplicationException("ROMProperties.csv: Required column missing: MD5");
            if (_columnPositions[Column.CartType] < 0)
                throw new ApplicationException("ROMProperties.csv: Required column missing: CartType");
            if (_columnPositions[Column.MachineType] < 0)
                throw new ApplicationException("ROMProperties.csv: Required column missing: MachineType");
            if (_columnPositions[Column.LController] < 0)
                throw new ApplicationException("ROMProperties.csv: Required column missing: LController");
            if (_columnPositions[Column.RController] < 0)
                throw new ApplicationException("ROMProperties.csv: Required column missing: RController");
        }

        static GameProgram CreateGameSettingsFromLine(string line)
        {
            var row = new string[13];
            var linesplit = line.Split(',');
            for (var i = 0; i < row.Length && i < linesplit.Length; i++)
                row[i] = linesplit[i];

            var md5 = row[_columnPositions[Column.MD5]];
            var gp = new GameProgram(md5)
            {
                Title         = _columnPositions[Column.Title]        >= 0 ? row[_columnPositions[Column.Title]]          : string.Empty,
                Manufacturer  = _columnPositions[Column.Manufacturer] >= 0 ? row[_columnPositions[Column.Manufacturer]]   : string.Empty,
                Author        = _columnPositions[Column.Author]       >= 0 ? row[_columnPositions[Column.Author]]         : string.Empty,
                Year          = _columnPositions[Column.Year]         >= 0 ? row[_columnPositions[Column.Year]]           : string.Empty,
                ModelNo       = _columnPositions[Column.ModelNo]      >= 0 ? row[_columnPositions[Column.ModelNo]]        : string.Empty,
                Rarity        = _columnPositions[Column.Rarity]       >= 0 ? row[_columnPositions[Column.Rarity]]         : string.Empty,
                CartType      = ToCartType(row[_columnPositions[Column.CartType]]),
                MachineType   = ToMachineType(row[_columnPositions[Column.MachineType]])
            };

            gp.LController = ToController(row[_columnPositions[Column.LController]]);
            gp.RController = ToController(row[_columnPositions[Column.RController]]);

            if (gp.LController == Controller.None)
                gp.LController = GetDefaultController(gp.MachineType);
            if (gp.RController == Controller.None)
                gp.RController = GetDefaultController(gp.MachineType);

            if (_columnPositions[Column.HelpUri] < row.Length)
            {
                string helpUri = row[_columnPositions[Column.HelpUri]];
                if (helpUri != null) helpUri = helpUri.Trim();
                if (helpUri != null && !helpUri.Length.Equals(0))
                    gp.HelpUri = helpUri;
            }

            return gp;
        }

        static Controller GetDefaultController(MachineType machineType)
        {
            switch (machineType)
            {
                case MachineType.A7800NTSC:
                case MachineType.A7800PAL:
                    return Controller.ProLineJoystick;
                default:
                    return Controller.Joystick;
            }
        }

        static Column ToColumn(string columnName)
        {
            Column result;
            return Enum.TryParse(columnName, true, out result) ? result : Column.None;
        }

        static CartType ToCartType(string cartType)
        {
            CartType result;
            return Enum.TryParse(cartType, true, out result) ? result : CartType.None;
        }

        static MachineType ToMachineType(string machineType)
        {
            MachineType result;
            return Enum.TryParse(machineType, true, out result) ? result : MachineType.None;
        }

        static Controller ToController(string controller)
        {
            Controller result;
            return Enum.TryParse(controller, true, out result) ? result : Controller.None;
        }

        string ComputeMD5Digest(byte[] bytes)
        {
            return (bytes != null) ? StringifyMD5(_cryptoProvider.ComputeHash(bytes)) : null;
        }

        string StringifyMD5(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 16)
                return string.Empty;
            _sb.Length = 0;
            for (var i = 0; i < 16; i++)
                _sb.AppendFormat("{0:x2}", bytes[i]);
            return _sb.ToString();
        }

        #endregion
    }
}
