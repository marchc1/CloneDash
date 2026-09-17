using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CloneDash.Unbeatable.Internal;


public class BeatmapParserEngine
{
	public void ReadBeatmap(string contents, ref Beatmap beatmap) {
		string[] array = Regex.Split(contents, Environment.NewLine);
		if (array[0] != "osu file format v14")
			throw new NotSupportedException();
		string text = "";
		int num = 1;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++) {
			string text2 = array2[i].Trim();
			if (!(text2 == "") && !text2.StartsWith("//")) {
				string text3;
				if (this.TrySectionName(text2, out text3)) {
					text = text3;
					num = 1;
				}
				else if (text != "") {
					try {
						this.ParseLine(text2, text, ref beatmap);
					}
					catch (Exception) {
					}
					num++;
				}
			}
		}
	}

	private bool TrySectionName(string line, out string sectionName) {
		if (line.StartsWith("[") && line.EndsWith("]")) {
			sectionName = line.Substring(1, line.Length - 2);
			return true;
		}
		sectionName = null;
		return false;
	}

	private void ParseLine(string line, string sectionName, ref Beatmap beatmap) {
		uint num = PrivateImplementationDetails.ComputeStringHash(sectionName);
		if (num <= 1231601560U) {
			if (num <= 142628502U) {
				if (num == 3416228U) {
					return;
				}
				if (num != 142628502U) {
					return;
				}
				if (!(sectionName == "HitObjects")) {
					return;
				}
				this.ParseLineHitObjects(line, ref beatmap.hitObjects, ref beatmap.flips, ref beatmap.notes, ref beatmap.commands);
			}
			else {
				if (num == 438959914U) {
					return;
				}
				if (num != 1231601560U) {
					return;
				}
				return;
			}
		}
		else if (num <= 1960692340U) {
			if (num != 1432485131U) {
				if (num != 1960692340U) {
					return;
				}
				if (!(sectionName == "TimingPoints")) {
					return;
				}
				this.ParseLineTimingPoints(line, ref beatmap.timingPoints);
				return;
			}
			else {
				if (!(sectionName == "General")) {
					return;
				}
				this.ParseLineGeneral(line, ref beatmap.general);
				return;
			}
		}
		else if (num != 2166136261U) {
			if (num == 2233508368U) {
				return;
			}
			if (num != 2617504096U) {
				return;
			}
			if (!(sectionName == "Metadata")) {
				return;
			}
			this.ParseLineMetadata(line, ref beatmap.metadata);
			return;
		}
		else if (sectionName != null) {
			int length = sectionName.Length;
			return;
		}
	}

	private void ParseLineGeneral(string line, ref GeneralInfo general) {
		string[] array = line.Split(':', StringSplitOptions.None);
		string text = array[0];
		string text2 = array[1].Substring(1);
		uint num = PrivateImplementationDetails.ComputeStringHash(text);
		if (num <= 1397651250U) {
			if (num <= 906899804U) {
				if (num == 749010473U) {
					return;
				}
				if (num != 906899804U) {
					return;
				}
				if (!(text == "AudioFilename")) {
					return;
				}
				general.audioFilename = text2;
				return;
			}
			else if (num != 1037595928U) {
				if (num == 1192550549U) {
					return;
				}
				if (num != 1397651250U) {
					return;
				}
				return;
			}
			else {
				if (!(text == "AudioLeadIn")) {
					return;
				}
				general.audioLeadIn = int.Parse(text2);
				return;
			}
		}
		else if (num <= 2167992445U) {
			if (num != 1665405120U) {
				if (num != 2167992445U) {
					return;
				}
				return;
			}
			else {
				if (!(text == "Countdown")) {
					return;
				}
				general.countdown = int.Parse(text2);
				return;
			}
		}
		else if (num != 2576849862U) {
			if (num == 3626434118U) {
				return;
			}
			if (num != 4088792625U) {
				return;
			}
			return;
		}
		else {
			if (!(text == "PreviewTime")) {
				return;
			}
			general.previewTime = int.Parse(text2);
			return;
		}
	}
	
	private void ParseLineMetadata(string line, ref MetadataInfo metadata) {
		string[] array = line.Split(':', StringSplitOptions.None);
		string text = array[0];
		string text2 = array[1];
		uint num = PrivateImplementationDetails.ComputeStringHash(text);
		if (num <= 1992138944U) {
			if (num <= 1573770551U) {
				if (num != 617902505U) {
					if (num != 1573770551U) {
						return;
					}
					if (!(text == "Version")) {
						return;
					}
					metadata.version = text2;
					return;
				}
				else {
					if (!(text == "Title")) {
						return;
					}
					metadata.title = text2;
					return;
				}
			}
			else if (num != 1642243064U) {
				if (num == 1711094138U) {
					return;
				}
				if (num != 1992138944U) {
					return;
				}
				return;
			}
			else {
				if (!(text == "Source")) {
					return;
				}
				metadata.source = text2;
				return;
			}
		}
		else if (num <= 2490816510U) {
			if (num != 2258803444U) {
				if (num != 2490816510U) {
					return;
				}
				if (!(text == "Artist")) {
					return;
				}
				metadata.artist = text2;
				return;
			}
			else {
				if (!(text == "TitleUnicode")) {
					return;
				}
				metadata.titleUnicode = text2;
				return;
			}
		}
		else if (num != 3198710089U) {
			if (num == 3382753380U) {
				return;
			}
			if (num != 4291165879U) {
				return;
			}
			if (!(text == "Creator")) {
				return;
			}
			metadata.creator = text2;
			return;
		}
		else {
			if (!(text == "ArtistUnicode")) {
				return;
			}
			metadata.artistUnicode = text2;
			return;
		}
	}

	private void ParseLineEvents(string line, ref List<EventInfo> events) {
		string[] array = line.Split(new char[] { ',' }, 3);
		events.Add(new EventInfo {
			eventType = array[0],
			startTime = int.Parse(array[1]),
			eventParams = array[2].Split(',', StringSplitOptions.None)
		});
	}

	private void ParseLineTimingPoints(string line, ref List<TimingPointInfo> timingPoints) {
		string[] array = line.Split(',', StringSplitOptions.None);
		timingPoints.Add(new TimingPointInfo {
			time = (int)float.Parse(array[0], CultureInfo.InvariantCulture),
			beatLength = float.Parse(array[1], CultureInfo.InvariantCulture),
			meter = int.Parse(array[2]),
			sampleSet = int.Parse(array[3]),
			sampleIndex = int.Parse(array[4]),
			volume = int.Parse(array[5]),
			uninherited = (int.Parse(array[6]) != 0),
			effects = int.Parse(array[7])
		});
	}

	private void ParseLineHitObjects(string line, ref List<HitObjectInfo> hitObjects, ref List<FlipInfo> flips, ref List<NoteInfo> notes, ref List<CommandInfo> commands) {
		string[] array = line.Split(new char[] { ',' }, 6);
		string[] array2 = array[5].Split(new char[] { ':' }, 2);
		HitObjectInfo hitObjectInfo = new HitObjectInfo {
			x = int.Parse(array[0]),
			y = int.Parse(array[1]),
			time = int.Parse(array[2]),
			type = int.Parse(array[3]),
			hitSound = (HitObjectInfo.HitSound)int.Parse(array[4]),
			objectParams = array2[0].Split(',', StringSplitOptions.None),
			hitSample = array2[1].Split(':', StringSplitOptions.None)
		};
		if (hitObjectInfo.hitSample.Count<string>() < 5) {
			hitObjectInfo.hitSample = array[5].Split(':', StringSplitOptions.None);
		}
		hitObjects.Add(hitObjectInfo);
		if (hitObjectInfo.IsFlip()) {
			FlipInfo flipInfo = new FlipInfo(hitObjectInfo);
			flips.Add(flipInfo);
			if (!flipInfo.toggleCenter) {
				this.side = this.side.GetOpposite();
				this.lastFlip = flipInfo;
				return;
			}
		}
		else if (hitObjectInfo.IsAnyNote()) {
			NoteInfo noteInfo = new NoteInfo(hitObjectInfo, this.side);
			notes.Add(noteInfo);
			if (this.lastFlip != null) {
				if (this.lastFlip.nextNote == null) {
					this.lastFlip.nextNote = noteInfo;
					return;
				}
				if (this.lastFlip.nextNote.time == noteInfo.time) {
					this.lastFlip.twinNote = noteInfo;
					this.lastFlip = null;
					return;
				}
			}
		}
		else if (hitObjectInfo.IsCommand()) {
			commands.Add(new CommandInfo(hitObjectInfo, this.side));
		}
	}

	private Side side = Side.Right;

	private FlipInfo lastFlip;
}