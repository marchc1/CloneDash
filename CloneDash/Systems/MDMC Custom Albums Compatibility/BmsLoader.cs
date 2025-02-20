using AssetStudio;
using CloneDash.Systems.Muse_Dash_Compatibility;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using Nucleus;
using System;
using System.Diagnostics;
using System.Resources;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using static CloneDash.MuseDashCompatibility;

namespace CloneDash.Systems.CustomCharts
{
	public static class BmsLoader
	{
		public static Bms LoadFromFile(string filepath) => Load(new FileStream(filepath, FileMode.Open, FileAccess.Read), Path.GetFileNameWithoutExtension(filepath));
		public static Bms Load(Stream stream, string bmsName) {
			var bpmDict = new Dictionary<string, float>();
			var notePercents = new Dictionary<int, JsonObject>();
			var dataList = new List<JsonObject>();
			var notesArray = new JsonArray();
			var info = new JsonObject();

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
				if (noteId == short.MaxValue) {
					Logs.Warn($"Cannot process full chart, there are too many objects. Max objects is {short.MaxValue}.");
					break;
				}

				var configData = node.ToMusicConfigData();
				if (configData.time < 0) continue;

				// Create a new note for each configData
				var newNote = new MusicData();
				newNote.objId = noteId++;
				newNote.tick = Decimal.Round(configData.time, 3);
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
				var endIndex = (int)(Decimal.Round(
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
			var geminiCache = new Dictionary<Decimal, List<MusicData>>();

			for (var i = 1; i < MusicDataManager.Data.Count; i++) {
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

		internal static StageInfo TransmuteData(Bms bms) {
			MusicDataManager.Clear();
			_delay = 0;

			var noteData = bms.GetNoteData();
			Logs.Info("Got note data");

			LoadMusicData(noteData);
			MusicDataManager.Sort();

			//ProcessBossData(bms);
			MusicDataManager.Sort();

			ProcessGeminis();

			// Process the delay for each MusicData
			foreach (var mData in MusicDataManager.Data) {
				if (mData.configData == null) continue;
				mData.tick -= _delay;
				mData.showTick = Decimal.Round(mData.tick - mData.dt, 2);
				if (mData.isLongPressType)
					mData.endIndex -= (int)(_delay / (Decimal)0.001f);
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
