﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.IO;
using Aura.Data;
using Aura.Shared.Database;
using Aura.Shared.Util.Configuration;

namespace Aura.Shared.Util
{
	/// <summary>
	/// General methods needed by all servers.
	/// </summary>
	public abstract class ServerMain
	{
		/// <summary>
		/// Tries to find aura root folder, and changes the working directory to it.
		/// Exits if not successful.
		/// </summary>
		public void NavigateToRoot()
		{
			// Go back max 2 folders, the bins should be in [aura]/bin/(Debug|Release)
			for (int i = 0; i < 3; ++i)
			{
				if (Directory.Exists("system"))
					return;

				Directory.SetCurrentDirectory("..");
			}

			Log.Error("Unable to find root directory.");
			CliUtil.Exit(1);
		}

		/// <summary>
		/// Tries to call conf's load method, exits on error.
		/// </summary>
		public void LoadConf(BaseConf conf)
		{
			Log.Info("Reading configuration...");

			try
			{
				conf.Load();
			}
			catch (Exception ex)
			{
				Log.Exception(ex, "Unable to read configuration. ({0})", ex.Message);
				CliUtil.Exit(1);
			}
		}

		/// <summary>
		/// Tries to initialize database, with the information from conf,
		/// exits on error.
		/// </summary>
		public virtual void InitDatabase(BaseConf conf)
		{
			Log.Info("Initializing database...");

			try
			{
				AuraDb.Instance.Init(conf.Database.Host, conf.Database.User, conf.Database.Pass, conf.Database.Db);
			}
			catch (Exception ex)
			{
				Log.Error("Unable to open database connection. ({0})", ex.Message);
				CliUtil.Exit(1);
			}
		}

		/// <summary>
		/// (Re-)Loads data files (db), exits on error.
		/// </summary>
		/// <remarks>
		/// Called on server start and with some reload commands.
		/// Should only load required data, e.g. Msgr Server doesn't
		/// need race data.
		/// </remarks>
		public void LoadData(DataLoad toLoad, bool reload)
		{
			Log.Info("Loading data...");

			try
			{
				if ((toLoad & DataLoad.Races) != 0)
				{
					this.LoadDb(AuraData.AncientDropDb, "db/ancient_drops.txt", reload);
					this.LoadDb(AuraData.RaceSkillDb, "db/race_skills.txt", reload);
					this.LoadDb(AuraData.SpeedDb, "db/speed.txt", reload, false);
					this.LoadDb(AuraData.RaceDb, "db/races.txt", reload);
				}

				if ((toLoad & DataLoad.StatsBase) != 0)
				{
					this.LoadDb(AuraData.StatsBaseDb, "db/stats_base.txt", reload);
				}

				if ((toLoad & DataLoad.StatsLevel) != 0)
				{
					this.LoadDb(AuraData.StatsLevelUpDb, "db/stats_levelup.txt", reload);
				}

				if ((toLoad & DataLoad.Motions) != 0)
				{
					this.LoadDb(AuraData.MotionDb, "db/motions.txt", reload);
				}

				if ((toLoad & DataLoad.Cards) != 0)
				{
					this.LoadDb(AuraData.CharCardSetDb, "db/charcardsets.txt", reload, false);
					this.LoadDb(AuraData.CharCardDb, "db/charcards.txt", reload);
				}

				if ((toLoad & DataLoad.Colors) != 0)
				{
					this.LoadDb(AuraData.ColorMapDb, "db/colormap.dat", reload);
				}

				if ((toLoad & DataLoad.Items) != 0)
				{
					this.LoadDb(AuraData.ItemDb, "db/items.txt", reload);
					this.LoadDb(AuraData.ChairDb, "db/chairs.txt", reload);
				}

				if ((toLoad & DataLoad.Skills) != 0)
				{
					this.LoadDb(AuraData.SkillRankDb, "db/skill_ranks.txt", reload, false);
					this.LoadDb(AuraData.SkillDb, "db/skills.txt", reload);
				}

				if ((toLoad & DataLoad.Regions) != 0)
				{
					this.LoadDb(AuraData.RegionDb, "db/regions.txt", reload);
					this.LoadDb(AuraData.RegionInfoDb, "db/regioninfo.dat", reload);
				}

				if ((toLoad & DataLoad.Shamala) != 0)
				{
					this.LoadDb(AuraData.ShamalaDb, "db/shamala.txt", reload);
				}

				if ((toLoad & DataLoad.PropDrops) != 0)
				{
					this.LoadDb(AuraData.PropDropDb, "db/prop_drops.txt", reload);
				}

				if ((toLoad & DataLoad.Exp) != 0)
				{
					this.LoadDb(AuraData.ExpDb, "db/exp.txt", reload);
				}

				if ((toLoad & DataLoad.Pets) != 0)
				{
					this.LoadDb(AuraData.PetDb, "db/pets.txt", reload);
				}

				if ((toLoad & DataLoad.Weather) != 0)
				{
					this.LoadDb(AuraData.WeatherDb, "db/weather.txt", reload);
				}

				if ((toLoad & DataLoad.Keywords) != 0)
				{
					this.LoadDb(AuraData.KeywordDb, "db/keywords.txt", reload);
				}

				if ((toLoad & DataLoad.Titles) != 0)
				{
					this.LoadDb(AuraData.TitleDb, "db/titles.txt", reload);
				}
			}
			catch (FileNotFoundException ex)
			{
				Log.Error(ex.Message);
				CliUtil.Exit(1);
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				CliUtil.Exit(1);
			}
		}

		/// <summary>
		/// Loads db, first from system, then from user.
		/// Logs problems as warnings.
		/// </summary>
		private void LoadDb(IDatabase db, string path, bool reload, bool log = true)
		{
			var systemPath = Path.Combine("system", path);
			var userPath = Path.Combine("user", path);

			// System
			{
				db.Load(systemPath, reload);

				foreach (var ex in db.Warnings)
					Log.Warning(ex.ToString());
			}

			// User
			{
				// It's okay if user dbs don't exist.
				if (File.Exists(userPath))
				{
					db.Load(userPath, false);

					foreach (var ex in db.Warnings)
						Log.Warning(ex.ToString());
				}
			}

			if (log)
				Log.Info("  done loading {0} entries from {1}", db.Count, Path.GetFileName(path));
		}

		/// <summary>
		/// Loads system and user localization files.
		/// </summary>
		public void LoadLocalization(BaseConf conf)
		{
			Log.Info("Loading localization ({0})...", conf.Localization.Language);

			// System
			try
			{
				Localization.Parse(string.Format("system/localization/{0}", conf.Localization.Language));
			}
			catch (FileNotFoundException ex)
			{
				Log.Warning("Unable to load localization: " + ex.Message);
			}

			// User
			try
			{
				Localization.Parse(string.Format("user/localization/{0}", conf.Localization.Language));
			}
			catch (FileNotFoundException)
			{
			}
		}
	}

	/// <summary>
	/// Used in LoadData, to specify which db files should be loaded.
	/// </summary>
	public enum DataLoad : uint
	{
		//Spawns = 0x01,
		Skills = 0x02,
		Races = 0x04,
		StatsBase = 0x08,
		StatsLevel = 0x10,
		Motions = 0x20,
		Cards = 0x40,
		Colors = 0x80,
		Items = 0x100,
		Regions = 0x200,
		Shamala = 0x400,
		PropDrops = 0x800,
		Exp = 0x1000,
		Pets = 0x2000,
		Weather = 0x4000,
		Keywords = 0x8000,
		Titles = 0x10000,

		All = 0xFFFFFFFF,

		LoginServer = Races | StatsBase | Cards | Colors | Items | Pets,
		ChannelServer = All,
		Npcs = Races,
	}
}
