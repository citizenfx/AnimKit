using CodeWalker;
using CodeWalker.GameFiles;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AnimKit.Core
{
    public class OnimFile
    {
        public string Name { get; set; } = string.Empty;
        public string[] Version { get; set; }
        public string[] Flags { get; set; }
        public int Frames { get; set; }
        public int SequenceFrameLimit { get; set; }
        public float Duration { get; set; }
        public uint _f10 { get; set; }
        public string[] ExtraFlags { get; set; }
        public int Sequences { get; set; }
        public int MaterialID { get; set; }
        public List<OnimSequence> SequenceList { get; set; } = new List<OnimSequence>();

        public void Load(string onimfile, AssetLoadType loadType = AssetLoadType.Full)
        {
            if (loadType == AssetLoadType.NoAnimations)
            {
                return;
            }

            Name = Path.GetFileNameWithoutExtension(onimfile).ToLowerInvariant();

            string[] lines = File.ReadAllLines(onimfile);

            int depth = 0;
            var spacedelim = new[] { ' ' };

            bool inanim = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                string tline = line.Trim();
                if (string.IsNullOrEmpty(tline)) continue; //blank line
                //if (tline.StartsWith("#")) continue; //commented out

                string[] parts = tline.Split(spacedelim, StringSplitOptions.RemoveEmptyEntries);

                if (tline.StartsWith("{"))
                {
                    depth++;
                    continue;
                }
                if (tline.StartsWith("}"))
                {
                    depth--;
                    if (inanim) { inanim = false; }
                    continue;
                }

                if (depth == 0)
                {
                    if (tline.StartsWith("Version"))
                    {
                        Version = parts;
                    }
                }

                if (depth == 1)
                {
                    var hasval = parts?.Length > 1;
                    var values = parts.Skip(1).ToArray();

                    if (tline.StartsWith("Flags"))
                    {
                        Flags = values;
                    }
                    else if (tline.StartsWith("Frames"))
                    {
                        Frames = hasval ? TryParseInt(values[0]) : 0;
                    }
                    else if (tline.StartsWith("SequenceFrameLimit"))
                    {
                        SequenceFrameLimit = hasval ? TryParseInt(values[0]) : 0;
                    }
                    else if (tline.StartsWith("Duration"))
                    {
                        Duration = hasval ? TryParseFloat(values[0]) : 0;
                    }
                    else if (tline.StartsWith("_f10"))
                    {
                        _f10 = hasval ? TryParseUInt(values[0]) : 0;
                    }
                    else if (tline.StartsWith("ExtraFlags"))
                    {
                        ExtraFlags = values;
                    }
                    else if (tline.StartsWith("Sequences"))
                    {
                        Sequences = hasval ? TryParseInt(values[0]) : 0;
                    }
                    else if (tline.StartsWith("MaterialID"))
                    {
                        MaterialID = hasval ? TryParseInt(values[0]) : 0;
                    }
                    else if (tline.StartsWith("Animation"))
                    {
                        inanim = true;
                    }
                    else
                    { }
                }

                if ((depth == 2) && inanim)
                {
                    OnimSequence seq = new OnimSequence();
                    if (seq.Read(lines, ref i))
                    {
                        SequenceList.Add(seq);
                    }
                }
            }
        }

        public void Load(Animation crAnim, string onimfile)
        {
            int[] seqs = new int[(crAnim.Frames / crAnim.SequenceFrameLimit) + 1];

            for (int s = 0; s < seqs.Length; s++)
            {
                seqs[s] = crAnim.SequenceFrameLimit + 1;
            }

            seqs[seqs.Length - 1] = crAnim.Frames % crAnim.SequenceFrameLimit;

            StringBuilder onimContent = new StringBuilder();

            onimContent.Append($@"Version 8 2
{{
	Flags FLAG_0 FLAG_4 FLAG_5 FLAG_6 FLAG_7 FLAG_8
	Frames {crAnim.Frames}
	SequenceFrameLimit {crAnim.SequenceFrameLimit}
	Duration {(crAnim.Frames / 30.0).ToString(CultureInfo.InvariantCulture)}
	_f10 285410817
	ExtraFlags
	Sequences {string.Join(" ", seqs.Select(f => f.ToString()))}
	MaterialID -1
	Animation
	{{
");

            int i = 0;

            foreach (var list in crAnim.BoneIds.data_items)
            {
                // for now, we only support BonePosition and BoneRotation tracks
                if (list.Track != 0 && list.Track != 1)
                {
                    continue;
                }

                bool isRotation = false;

                var chList = crAnim.Sequences.data_items[0].Sequences[i].Channels;

                if (chList.Length == 4)
                {
                    isRotation = true;
                }
                else if (chList.Length == 1)
                {
                    if (chList[0] is AnimChannelStaticQuaternion)
                    {
                        isRotation = true;
                    }
                }

                // temp placeholder
                if (list.Track == 1 && !isRotation)
                {
                    continue;
                }

                var type = (!isRotation) ? "BonePosition" : "BoneRotation";
                var dataType = (!isRotation) ? "Float3" : "Float4";

                var sb = new StringBuilder();

                bool failed = false;

                foreach (var seq in crAnim.Sequences.data_items)
                {
                    var channels = seq.Sequences[i].Channels;

                    // this will cause issues, we don't support unknown 5 channel types yet
                    if (channels.Length > 4)
                    {
                        failed = true;
                        break;
                    }

                    string ChannelType(AnimChannel chan)
                    {
                        if (seq.Sequences[i].IsType7Quat)
                        {
                            if (seq.Sequences[i].Channels[0] is AnimChannelStaticFloat && seq.Sequences[i].Channels[1] is AnimChannelStaticFloat && seq.Sequences[i].Channels[2] is AnimChannelStaticFloat)
                            {
                                return " Static";
                            }
                        }
                        else if (chan is AnimChannelStaticFloat || chan is AnimChannelStaticVector3 || chan is AnimChannelStaticQuaternion)
                        {
                            return " Static";
                        }

                        return "";
                    }

                    string ChannelData(AnimChannel chan, bool singleChannel)
                    {
                        var prefix = singleChannel ? "" : "	";

                        if (seq.Sequences[i].IsType7Quat)
                        {
                            // get index this weird way
                            int index = 0;

                            for (int j = 0; j < seq.Sequences[i].Channels.Length; j++)
                            {
                                if (seq.Sequences[i].Channels[j] == chan)
                                {
                                    index = j;
                                    break;
                                }
                            }

                            // actually we should only export Static for 'real' channels, but as mapping for these is stupid, we'll just repeat the same value even if one channel is supposed to be static
                            if (seq.Sequences[i].Channels[0] is AnimChannelStaticFloat && seq.Sequences[i].Channels[1] is AnimChannelStaticFloat && seq.Sequences[i].Channels[2] is AnimChannelStaticFloat)
                            {
                                var q = seq.Sequences[i].EvaluateQuaternionType7(0);

                                return $"{prefix}				{q[index].ToString(CultureInfo.InvariantCulture)}\r\n";
                            }

                            StringBuilder db = new StringBuilder();
                            for (int f = 0; f < seq.NumFrames; f++)
                            {
                                db.AppendLine($"{prefix}				{seq.Sequences[i].EvaluateQuaternionType7(f)[index].ToString(CultureInfo.InvariantCulture)}");
                            }

                            return db.ToString();
                        }

                        switch (chan)
                        {
                            case AnimChannelStaticFloat sf:
                                return $"{prefix}				{sf.Value.ToString(CultureInfo.InvariantCulture)}\r\n";
                            case AnimChannelStaticVector3 v3:
                                return $"{prefix}				{v3.Value[0].ToString(CultureInfo.InvariantCulture)} {v3.Value[1].ToString(CultureInfo.InvariantCulture)} {v3.Value[2].ToString(CultureInfo.InvariantCulture)}\r\n";
                            case AnimChannelStaticQuaternion q3:
                                return $"{prefix}				{q3.Value[0].ToString(CultureInfo.InvariantCulture)} {q3.Value[1].ToString(CultureInfo.InvariantCulture)} {q3.Value[2].ToString(CultureInfo.InvariantCulture)} {q3.Value[3].ToString(CultureInfo.InvariantCulture)}\r\n";
                            default:
                                {
                                    StringBuilder db = new StringBuilder();
                                    for (int f = 0; f < seq.NumFrames; f++)
                                    {
                                        db.AppendLine($"{prefix}				{chan.EvaluateFloat(f).ToString(CultureInfo.InvariantCulture)}");
                                    }

                                    return db.ToString();
                                }
                        }
                    }

                    if (channels.Length == 1)
                    {
                        var chanType = ChannelType(channels[0]);
                        var chanData = ChannelData(channels[0], true);


                        sb.Append($@"			FramesData SingleChannel{chanType}
			{{
{chanData}			}}
");
                    }
                    else
                    {
                        sb.Append($@"			FramesData MultiChannel
			{{
{string.Join("", channels.Select(ch => $@"				channel{ChannelType(ch)}
				{{
{ChannelData(ch, false)}				}}
"))}			}}
");
                    }
                }

                if (failed)
                {
                    continue;
                }

                var animBits = sb.ToString();

                onimContent.Append($@"		{type} {dataType} {list.BoneId}
		{{
{animBits}		}}
");

                i++;
            }

            onimContent.Append($@"	}}
}}
");

            File.WriteAllText(onimfile, onimContent.ToString());

            Load(onimfile);
        }

        public static int TryParseInt(string s)
        {
            int v = 0;
            int.TryParse(s, out v);
            return v;
        }

        public static uint TryParseUInt(string s)
        {
            uint v = 0;
            uint.TryParse(s, out v);
            return v;
        }

        public static float TryParseFloat(string s)
        {
            return FloatUtil.Parse(s);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class OnimSequence
    {
        public string Track { get; set; }
        public string Type { get; set; }
        public int BoneID { get; set; }
        public List<OnimFramesData> FramesData { get; set; } = new List<OnimFramesData>();

        public bool Read(string[] lines, ref int i)
        {
            var line = lines[i];
            string tline = line.Trim();
            if (string.IsNullOrEmpty(tline)) return false; //blank line
            //if (tline.StartsWith("#")) continue; //commented out
            var spacedelim = new[] { ' ' };
            string[] parts = tline.Split(spacedelim, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            { return false; }

            Track = parts[0];
            Type = parts[1];
            BoneID = OnimFile.TryParseInt(parts[2]);

            i++;

            int depth = 0;
            while (i < lines.Length)
            {
                line = lines[i];
                tline = line.Trim();
                if (string.IsNullOrEmpty(tline)) { i++; continue; }
                if (tline.StartsWith("{")) { depth++; }
                if (tline.StartsWith("}")) { depth--; }
                if (depth <= 0) { break; }

                if (depth == 1)
                {
                    if (tline.StartsWith("FramesData"))
                    {
                        OnimFramesData framesData = new OnimFramesData();
                        if (framesData.Read(lines, ref i))
                        {
                            if (framesData.Type == "SingleChannel" && !framesData.IsStatic)
                            {
                                var count = Type == "Float3" ? 3 : 4;

                                var oldFramesData = framesData;
                                framesData = new OnimFramesData();
                                framesData.Type = "MultiChannel";
                                framesData.IsStatic = oldFramesData.IsStatic;

                                var vs = oldFramesData.Channels[0].Values;
                                var len = vs.Length;

                                for (int ch = 0; ch < count; ch++)
                                {
                                    var odc = new OnimFramesDataChannel();
                                    odc.Values = new float[len / count];

                                    var outC = 0;

                                    for (int inC = ch; inC < len; inC += count, outC++)
                                    {
                                        odc.Values[outC] = vs[inC];
                                    }

                                    framesData.Channels.Add(odc);
                                }
                            }

                            FramesData.Add(framesData);
                        }
                    }
                }

                i++;
            }

            return true;
        }

        public override string ToString()
        {
            return Track + " " + Type + " " + BoneID.ToString();
        }
    }

    public class OnimFramesData
    {
        public string Type { get; set; }
        public bool IsStatic { get; set; }
        public List<OnimFramesDataChannel> Channels { get; set; } = new List<OnimFramesDataChannel>();

        public bool Read(string[] lines, ref int i)
        {
            var line = lines[i];
            string tline = line.Trim();

            if (string.IsNullOrEmpty(tline)) return false; //blank line

            //if (tline.StartsWith("#")) continue; //commented out

            var spacedelim = new[] { ' ' };
            string[] parts = tline.Split(spacedelim, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            { return false; }

            Type = parts[1];
            IsStatic = ((parts.Length > 2) && (parts[2] == "Static"));

            i++;

            int depth = 0;
            while (i < lines.Length)
            {
                line = lines[i];
                tline = line.Trim();
                if (string.IsNullOrEmpty(tline)) { i++; continue; }
                if (tline.StartsWith("{")) { depth++; }
                if (tline.StartsWith("}")) { depth--; }
                if (depth <= 0) { break; }

                if (Type == "SingleChannel")
                {
                    OnimFramesDataChannel chan = new OnimFramesDataChannel();
                    if (chan.Read(lines, ref i))
                    {
                        Channels.Add(chan);
                        i--;
                    }
                }
                else if (Type == "MultiChannel")
                {
                    if (tline.StartsWith("channel"))
                    {
                        i++;
                        OnimFramesDataChannel chan = new OnimFramesDataChannel();
                        if (chan.Read(lines, ref i))
                        {
                            Channels.Add(chan);
                        }
                    }
                }
                else
                { }

                i++;
            }


            return true;
        }

        public override string ToString()
        {
            return Type + " " + (IsStatic ? "Static " : "") + "(" + (Channels?.Count ?? 0).ToString() + " channels)";
        }

    }

    public class OnimFramesDataChannel
    {
        public float[] Values { get; set; }

        public bool Read(string[] lines, ref int i)
        {
            List<float> vals = new List<float>();

            var spacedelim = new[] { ' ' };
            int depth = 0;
            while (i < lines.Length)
            {
                var line = lines[i];
                var tline = line.Trim();
                i++;
                if (string.IsNullOrEmpty(tline)) continue;
                if (tline.StartsWith("{")) { depth++; continue; }
                if (tline.StartsWith("}")) { depth--; }
                if (depth <= 0)
                { i--; break; }

                if (depth == 1)
                {
                    string[] parts = tline.Split(spacedelim, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        if (string.IsNullOrEmpty(part)) continue;
                        var val = OnimFile.TryParseFloat(part);
                        vals.Add(val);
                    }
                }
                else
                { return false; }

            }

            Values = vals.ToArray();

            return true;
        }

        public override string ToString()
        {
            return (Values?.Length ?? 0).ToString() + " values";
        }
    }
}
