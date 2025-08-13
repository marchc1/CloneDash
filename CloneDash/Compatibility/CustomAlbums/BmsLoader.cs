using CloneDash.Compatibility.MuseDash;
using CloneDash.Game;

using Nucleus;

using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace CloneDash.Compatibility.CustomAlbums
{
	public static class BmsLoader
	{
		public static Bms Load(Stream stream, string bmsName, out List<TempoChange> bpmChanges) {
			var bpmDict = new Dictionary<string, float>();
			var notePercents = new Dictionary<int, JsonObject>();
			var dataList = new List<JsonObject>();
			var notesArray = new JsonArray();
			var info = new JsonObject();
			bpmChanges = [];

			using var streamReader = new StreamReader(stream);
			while (streamReader.ReadLine()?.Trim() is { } line) {
				if (string.IsNullOrEmpty(line) || !line.StartsWith("#")) continue;

				// Remove # from beginning of line
				line = line[1..];

				if (line.Contains(' ')) {
					// Parse header
					var split = line.Split(' ');
					var key = split[0];
					var value = split[1];

					info[key] = value;

					if (!key.Contains("BPM")) continue;

					var bpmKey = string.IsNullOrEmpty(key[3..]) ? "00" : key[3..];
					bpmDict.Add(bpmKey, float.Parse(value));

					if (bpmKey != "00") continue;

					var freq = 60f / float.Parse(value) * 4f;
					var obj = new JsonObject
					{
						{ "tick", 0f },
						{ "freq", freq }
					};
					dataList.Add(obj);
				}
				else if (line.Contains(':')) {
					// Parse data field
					var split = line.Split(':');
					var key = split[0];
					var value = split[1];

					var beat = int.Parse(key[..3]);
					var typeCode = key.Substring(3, 2);

					if (!Bms.Channels.TryGetValue(typeCode, out var type)) continue;

					if (type is Bms.ChannelType.SpTimesig) {
						var obj = new JsonObject
						{
							{ "beat", beat },
							{ "percent", float.Parse(value) }
						};
						notePercents.Add(beat, obj);
					}
					else {
						var objLength = value.Length / 2;
						for (var i = 0; i < objLength; i++) {
							var note = value.Substring(i * 2, 2);
							if (note is "00") continue;

							var tick = (float)i / objLength + beat;

							if (type is Bms.ChannelType.SpBpmDirect or Bms.ChannelType.SpBpmLookup) {
								// Handle BPM changes
								var freqDivide = type == Bms.ChannelType.SpBpmLookup &&
												 bpmDict.TryGetValue(note, out var bpm)
									? bpm
									: Convert.ToInt32(note, 16);
								var freq = 60f / freqDivide * 4f;

								var obj = new JsonObject
								{
									{ "tick", tick },
									{ "freq", freq }
								};
								dataList.Add(obj);
								dataList.Sort((l, r) => {
									var tickL = l["tick"].GetValue<float>();
									var tickR = r["tick"].GetValue<float>();

									return tickR.CompareTo(tickL);
								});
								bpmChanges.Add(new(tick, tick + 1, freqDivide));
							}
							else {
								// Parse other note data
								var time = 0f; // num3
								var totalOffset = 0f; // num4

								var data = dataList.FindAll(d => d["tick"].GetValue<float>() < tick);
								for (var j = data.Count - 1; j >= 0; j--) {
									var obj = data[j];
									var offset = 0f; // num5
									var freq = obj["freq"].GetValue<float>(); // num6

									if (j - 1 >= 0) {
										var prevObj = data[j - 1];
										offset = prevObj["tick"].GetValue<float>() - obj["tick"].GetValue<float>();
									}

									if (j == 0) offset = tick - obj["tick"].GetValue<float>();

									var localOffset = totalOffset; // num7
									totalOffset += offset;
									var floorOffset = NMath.FloorToInt(localOffset); // num8
									var ceilOffset = NMath.CeilToInt(totalOffset); // num9

									for (var k = floorOffset; k < ceilOffset; k++) {
										var off = 1f; // num10

										if (k == floorOffset)
											off = k + 1 - localOffset;
										if (k == ceilOffset - 1)
											off = totalOffset - (ceilOffset - 1);
										if (ceilOffset == floorOffset + 1)
											off = totalOffset - localOffset;

										notePercents.TryGetValue(k, out var node);
										var percent = node?["percent"].GetValue<float>() ?? 1f;
										time += NMath.RoundToInt(off * percent * freq / 1E-06f) * 1E-06F;
									}
								}

								var noteObj = new JsonObject
								{
									{ "time", time },
									{ "value", note },
									{ "tone", typeCode }
								};
								notesArray.Add(noteObj);
							}
						}
					}
				}
			}

			var list = notesArray.ToList();
			list.Sort((l, r) => {
				var lTime = l["time"]!.GetValue<float>();
				var rTime = r["time"]!.GetValue<float>();
				var lTone = l["tone"]!.GetValue<string>();
				var rTone = r["tone"]!.GetValue<string>();

				// Accurate for note sorting up to 6 decimal places
				var lScore = (long)(lTime * 1000000) * 10 + (lTone == "15" ? 0 : 1);
				var rScore = (long)(rTime * 1000000) * 10 + (rTone == "15" ? 0 : 1);

				return Math.Sign(lScore - rScore);
			});

			notesArray.Clear();
			list.ForEach(notesArray.Add);

			var percentsArray = new JsonArray();
			notePercents.Values.ToList().ForEach(percentsArray.Add);
			var bms = new Bms {
				Info = info,
				Notes = notesArray,
				NotesPercent = percentsArray,
				Md5 = Convert.ToHexString(MD5.HashData(stream))
			};
			bms.Info["NAME"] = bmsName;
			bms.Info["NEW"] = true;

			if (bms.Info.TryGetPropertyValue("BANNER", out var banner))
				bms.Info["BANNER"] = "cover/" + banner;
			else
				bms.Info["BANNER"] = "cover/none_cover.png";

			Logs.Info($"Loaded bms {bmsName}.");

			return bms;
		}

		private static decimal _delay;
		private static void LoadMusicData(JsonArray noteData) {
			short noteId = 1;
			foreach (var node in noteData) {
				Interlude.Spin(submessage: "Reading Custom Albums chart...");
				if (noteId == short.MaxValue) {
					Logs.Warn($"Cannot process full chart, there are too many objects. Max objects is {short.MaxValue}.");
					break;
				}

				var configData = node.ToMusicConfigData();
				if (configData.time < 0) continue;

				// Create a new note for each configData
				var newNote = new MusicData();
				newNote.objId = noteId++;
				newNote.tick = decimal.Round(configData.time, 3);
				newNote.configData = configData;
				newNote.isLongPressEnd = false;
				newNote.isLongPressing = false;

				if (MuseDashCompatibility.UIDToNote.TryGetValue(newNote.configData.note_uid, out var newNoteData)) {
					newNote.noteData = newNoteData;
					// todo; static scrollspeed
					// this works to cope with that for now...
					newNote.dt = newNoteData.speed;
				}

				MusicDataManager.Add(newNote);

				// Create ticks for hold notes. If it isn't a hold note, there is no need to continue.
				if (!newNote.isLongPressStart) continue;

				// Calculate the index in which the hold note ends
				var endIndex = (int)(decimal.Round(
					newNote.tick + newNote.configData.length - newNote.noteData.left_great_range -
					newNote.noteData.left_perfect_range,
					3) / 0.001m);

				for (var i = 1; i <= newNote.longPressCount; i++) {
					var holdTick = new MusicData();
					holdTick.objId = noteId++;
					holdTick.tick = i == newNote.longPressCount
						? newNote.tick + newNote.configData.length
						: newNote.tick + 0.1m * i;
					holdTick.configData = newNote.configData;

					// ACTUALLY REQUIRED TO WORK
					var dataCopy = holdTick.configData.Copy();
					dataCopy.length = 0;
					holdTick.configData = dataCopy;

					holdTick.isLongPressing = i != newNote.longPressCount;
					holdTick.isLongPressEnd = i == newNote.longPressCount;
					holdTick.noteData = newNote.noteData;
					holdTick.longPressPTick = newNote.configData.time;
					holdTick.endIndex = endIndex;

					MusicDataManager.Add(holdTick);
				}
			}

			Logs.Info("Loaded music data!");
		}

		private static void ProcessGeminis() {
			var geminiCache = new Dictionary<decimal, List<MusicData>>();

			for (var i = 1; i < MusicDataManager.Data.Count; i++) {
				Interlude.Spin(submessage: "Reading Custom Albums chart...");
				var mData = MusicDataManager.Data[i];
				mData.doubleIdx = -1;
				MusicDataManager.Set(i, mData);

				// if (mData.noteData.GetNoteType() != NoteType.Monster && mData.noteData.GetNoteType() != NoteType.Hide)
				//  	continue;

				if (geminiCache.TryGetValue(mData.tick, out var geminiList)) {
					var isNoteGemini = Bms.BmsIds[mData.noteData.ibms_id ?? "00"] == Bms.BmsId.Gemini;
					var isTargetGemini = false;
					var target = new MusicData();

					foreach (var gemini in geminiList.Where(gemini => mData.isAir != gemini.isAir)) {
						target = gemini;
						isTargetGemini = Bms.BmsIds[gemini.noteData.ibms_id ?? "00"] == Bms.BmsId.Gemini;

						if (isNoteGemini && isTargetGemini) break;
						if (!isNoteGemini) break;
					}

					if (target.objId > 0) {
						mData.isDouble = isNoteGemini && isTargetGemini;
						mData.doubleIdx = target.objId;
						target.isDouble = isNoteGemini && isTargetGemini;
						target.doubleIdx = mData.objId;

						MusicDataManager.Set(mData.objId, mData);
						MusicDataManager.Set(target.objId, target);
					}
				}
				else {
					geminiCache[mData.tick] = new List<MusicData>();
				}

				geminiCache[mData.tick].Add(mData);
			}

			Logs.Info("Processed geminis!");
		}
		public enum AnimAlignment
		{
			Left = -1,
			Right = 1
		}

		public enum BossState
		{
			OffScreen,
			Idle,
			Phase1,
			Phase2
		}

		public static readonly Dictionary<string, BossState> AnimStatesLeft = new()
		{
			{ "in", BossState.OffScreen },
			{ "out", BossState.Idle },
			{ "boss_close_atk_1", BossState.Idle },
			{ "boss_close_atk_2", BossState.Idle },
			{ "multi_atk_48", BossState.Idle },
			{ "multi_atk_48_end", BossState.Idle },
			{ "boss_far_atk_1_L", BossState.Phase1 },
			{ "boss_far_atk_1_R", BossState.Phase1 },
			{ "boss_far_atk_2", BossState.Phase2 },
			{ "boss_far_atk_1_start", BossState.Idle },
			{ "boss_far_atk_2_start", BossState.Idle },
			{ "boss_far_atk_1_end", BossState.Phase1 },
			{ "boss_far_atk_2_end", BossState.Phase2 },
			{ "atk_1_to_2", BossState.Phase1 },
			{ "atk_2_to_1", BossState.Phase2 }
		};

		public static readonly Dictionary<string, BossState> AnimStatesRight = new()
		{
			{ "in", BossState.Idle },
			{ "out", BossState.OffScreen },
			{ "boss_close_atk_1", BossState.Idle },
			{ "boss_close_atk_2", BossState.Idle },
			{ "multi_atk_48", BossState.Idle },
			{ "multi_atk_48_end", BossState.OffScreen },
			{ "boss_far_atk_1_L", BossState.Phase1 },
			{ "boss_far_atk_1_R", BossState.Phase1 },
			{ "boss_far_atk_2", BossState.Phase2 },
			{ "boss_far_atk_1_start", BossState.Phase1 },
			{ "boss_far_atk_2_start", BossState.Phase2 },
			{ "boss_far_atk_1_end", BossState.Idle },
			{ "boss_far_atk_2_end", BossState.Idle },
			{ "atk_1_to_2", BossState.Phase2 },
			{ "atk_2_to_1", BossState.Phase1 }
		};

		public static readonly Dictionary<BossState, Dictionary<BossState, string>> StateTransferAnims = new()
		{
			{
				BossState.OffScreen, new Dictionary<BossState, string>
				{
					{ BossState.OffScreen, "0" },
					{ BossState.Idle, "in" },
					{ BossState.Phase1, "boss_far_atk_1_start" },
					{ BossState.Phase2, "boss_far_atk_2_start" }
				}
			},
			{
				BossState.Idle, new Dictionary<BossState, string>
				{
					{ BossState.OffScreen, "out" },
					{ BossState.Idle, "0" },
					{ BossState.Phase1, "boss_far_atk_1_start" },
					{ BossState.Phase2, "boss_far_atk_2_start" }
				}
			},
			{
				BossState.Phase1, new Dictionary<BossState, string>
				{
					{ BossState.OffScreen, "out" },
					{ BossState.Idle, "boss_far_atk_1_end" },
					{ BossState.Phase1, "0" },
					{ BossState.Phase2, "atk_1_to_2" }
				}
			},
			{
				BossState.Phase2, new Dictionary<BossState, string>
				{
					{ BossState.OffScreen, "out" },
					{ BossState.Idle, "boss_far_atk_2_end" },
					{ BossState.Phase1, "atk_2_to_1" },
					{ BossState.Phase2, "0" }
				}
			}
		};

		public static readonly Dictionary<string, AnimAlignment> TransferAlignment = new()
		{
			{ "in", AnimAlignment.Right },
			{ "out", AnimAlignment.Left },
			{ "boss_far_atk_1_start", AnimAlignment.Right },
			{ "boss_far_atk_2_start", AnimAlignment.Right },
			{ "boss_far_atk_1_end", AnimAlignment.Left },
			{ "boss_far_atk_2_end", AnimAlignment.Left },
			{ "atk_1_to_2", AnimAlignment.Right },
			{ "atk_2_to_1", AnimAlignment.Right }
		};
		public static bool IsBossNote(this MusicData mData) => !string.IsNullOrEmpty(mData.noteData?.boss_action) && mData.noteData?.boss_action != "0";

		internal static void ProcessBossData(Bms bms) {
			var bossData = MusicDataManager.Data.Where(mData => mData.IsBossNote()).ToList();
			if (bossData.Count == 0) 
				return;

			// TODO: Add a boss exit animation if it is missing.
			// Need to convert the code they use for MD to use Model4System animation stuff

			// Incorrect phase gears
			var phaseGearConfig = new NoteConfigData();
			phaseGearConfig.ibms_id = "";

			for (var i = 0; i < bossData.Count; i++) {
				var data = bossData[i]!;
				var thisNoteData = data.noteData!;

				if (thisNoteData.GetNoteType() != NoteType.Block) continue;

				// Find the next boss animation that is not a gear
				var bossAnimAhead = new MusicData() {
					configData = new MusicConfigData()
				};
				bossAnimAhead.configData.time = Decimal.MinValue;

				for (var j = i + 1; j < bossData.Count; j++) {
					var dataAhead = bossData[j]!;
					if (dataAhead.noteData!.GetNoteType() == NoteType.Block) continue;

					bossAnimAhead = dataAhead;
					break;
				}

				MusicData bossAnimBefore;
				if (i > 0) {
					bossAnimBefore = bossData[i - 1];
				}
				else {
					bossAnimBefore = new MusicData() {
						configData = new MusicConfigData()
					};
					bossAnimBefore.configData.time = Decimal.MinValue;
				}

				var diffToAhead = Math.Abs((float)data.configData.time - (float)bossAnimAhead.configData.time);
				var diffToBefore = Math.Abs((float)data.configData.time - (float)bossAnimBefore.configData.time);
				var ahead = diffToAhead < diffToBefore;

				if (bossAnimBefore.noteData == null || bossAnimAhead.noteData == null) {
					Logs.Warn("Ran into weird case in BmsLoader where bossAnimBefore or bossAnimAhead were missing noteData...?");
				}

				var stateBehind = i > 0 ? AnimStatesRight[bossAnimBefore.noteData.boss_action] : BossState.OffScreen;
				var stateAhead = AnimStatesLeft.TryGetValue(bossAnimAhead.noteData.boss_action, out var state)
					? state
					: BossState.OffScreen;
				var usedState = ahead ? stateAhead : stateBehind;
				var correctState = usedState is BossState.Phase1 or BossState.Phase2;
				if (!correctState) {
					ahead = !ahead;
					usedState = ahead ? stateAhead : stateBehind;
					correctState = usedState is BossState.Phase1 or BossState.Phase2;
				}

				if (!correctState) continue;
				if (usedState is not (BossState.Phase1 or BossState.Phase2)) continue;

				if ((ahead && AnimStatesLeft[data.noteData.boss_action] == usedState) ||
					(!ahead && AnimStatesRight[data.noteData.boss_action] == usedState))
					continue;

				var phase = usedState == BossState.Phase1 ? 1 : 2;

				var noteData = MuseDashCompatibility.NoteDataManager;
				if (phaseGearConfig.ibms_id != data.noteData.ibms_id
					|| phaseGearConfig.pathway != data.noteData.pathway
					|| phaseGearConfig.scene != data.noteData.scene
					|| phaseGearConfig.speed != data.noteData.speed
					|| !phaseGearConfig.boss_action.StartsWith($"boss_far_atk_{phase}"))
					foreach (var d in noteData) {
						if (d.ibms_id != data.noteData.ibms_id
							|| d.pathway != data.noteData.pathway
							|| d.scene != data.noteData.scene
							|| d.speed != data.noteData.speed
							|| !d.boss_action.StartsWith($"boss_far_atk_{phase}")) continue;

						phaseGearConfig = d;
						break;
					}

				var fixedConfigData = new MusicConfigData();
				fixedConfigData.blood = data.configData.blood;
				fixedConfigData.id = data.configData.id;
				fixedConfigData.length = data.configData.length;
				fixedConfigData.note_uid = phaseGearConfig.uid;
				fixedConfigData.pathway = data.configData.pathway;
				fixedConfigData.time = data.configData.time;

				var fixedGear = new MusicData();
				fixedGear.objId = data.objId;
				fixedGear.tick = data.tick;
				fixedGear.configData = fixedConfigData;
				fixedGear.isLongPressEnd = data.isLongPressEnd;
				fixedGear.isLongPressing = data.isLongPressing;
				fixedGear.noteData = phaseGearConfig;

				MusicDataManager.Set(fixedGear.objId, fixedGear);
				bossData[i] = fixedGear;
				Logs.Info($"Customs: fixed gear at tick {data.tick}");
			}
		}
		internal static StageInfo TransmuteData(Bms bms) {
			MusicDataManager.Clear();
			_delay = 0;

			var noteData = bms.GetNoteData();
			Logs.Info("Got note data");

			LoadMusicData(noteData); Interlude.Spin(submessage: "Reading Custom Albums chart...");
			MusicDataManager.Sort(); Interlude.Spin(submessage: "Reading Custom Albums chart...");

			ProcessBossData(bms);
			MusicDataManager.Sort(); Interlude.Spin(submessage: "Reading Custom Albums chart...");

			ProcessGeminis(); Interlude.Spin(submessage: "Reading Custom Albums chart...");

			// Process the delay for each MusicData
			foreach (var mData in MusicDataManager.Data) {
				Interlude.Spin(submessage: "Reading Custom Albums chart...");
				if (mData.configData == null) continue;
				mData.tick -= _delay;
				mData.showTick = decimal.Round(mData.tick - mData.dt, 2);
				if (mData.isLongPressType)
					mData.endIndex -= (int)(_delay / (decimal)0.001f);
			}

			// Transmute the MusicData to a new StageInfo object
			var stageInfo = new StageInfo();
			stageInfo.musicDatas = [.. MusicDataManager.Data];
			stageInfo.delay = _delay;

			MusicDataManager.Clear();
			return stageInfo;
		}
	}
}
