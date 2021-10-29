using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CodeWalker.GameFiles;
using SharpDX;

namespace AnimKit.Core
{
    public class OadFile
    {
        public string Name { get; set; } = string.Empty;

        public string FileDirectory { get; set; } = string.Empty;

        public string[] Version { get; set; }

        public List<OnimFile> OnimFiles { get; set; } = new List<OnimFile>();

        public List<string> OnimPaths { get; set; } = new List<string>();

        public void Load(string oadfile, AssetLoadType loadType = AssetLoadType.Full, Action<int, int> loadProgress = null)
        {
            FileDirectory = Path.GetDirectoryName(oadfile);
            Name = Path.GetFileNameWithoutExtension(oadfile);

            var basePath = FileDirectory + "\\";

            if (!Directory.Exists(basePath))
            {
                _ = Directory.CreateDirectory(basePath);
            }

            if (!File.Exists(oadfile))
            {
                return;
            }

            string[] lines = File.ReadAllLines(oadfile);

            int depth = 0;
            char[] spacedelim = new[] { ' ' };

            OnimPaths.Clear();

            foreach (var line in lines)
            {
                string tline = line.Trim();
                if (string.IsNullOrEmpty(tline)) continue; // blank line
                //if (tline.StartsWith("#")) continue; // commented out

                string[] parts = tline.Split(spacedelim, StringSplitOptions.RemoveEmptyEntries);

                if (tline.StartsWith("{")) { depth++; continue; }
                if (tline.StartsWith("}")) { depth--; } // need to handle the closing cases

                if (depth == 0)
                {
                    if (tline.StartsWith("Version"))
                    {
                        Version = parts;
                    }
                }

                if (depth == 1)
                {
                    if (tline.StartsWith("crAnimation") && (tline.Length > 11))
                    {
                        OnimPaths.Add(tline.Substring(11).Trim());
                    }
                }
            }

            OnimFiles.Clear();

            if (loadType == AssetLoadType.NoAnimations)
            {
                return;
            }

            int onimCount = OnimPaths.Count;
            int onimLoaded = 0;

            foreach (string onimName in OnimPaths)
            {
                string onimPath = Path.Combine(basePath, onimName);

                if (!File.Exists(onimPath))
                {
                    continue;
                }

                OnimFile onimfile = new OnimFile();
                onimfile.Load(onimPath, loadType);

                OnimFiles.Add(onimfile);
                onimLoaded++;

                loadProgress?.Invoke(onimLoaded, onimCount);
            }
        }

        public void Save(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            if (string.IsNullOrEmpty(Name))
            {
                Name = $"dictionary";
            }

            string oadcontent = "Version 1 2\n{\n";

            foreach (var onim in OnimFiles)
            {
                oadcontent += $"	crAnimation {Name}\\{onim.Name}.onim\n";
            }

            oadcontent += "}\n";

            File.WriteAllText(Path.Combine(folder, $"{Name}.oad"), oadcontent);
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(FileDirectory))
            {
                return;
            }

            Save(FileDirectory);
        }

        public void RemoveOnim(OnimFile onimFile)
        {
            string onimPath = Path.Combine(FileDirectory, Name, onimFile.Name + ".onim");

            if (File.Exists(onimPath))
            {
                File.Delete(onimPath);
            }

            _ = OnimPaths.Remove(onimPath);
            _ = OnimFiles.Remove(onimFile);
        }

        public bool RenameOnim(OnimFile onimFile, string targetName)
        {
            string onimPath = Path.Combine(FileDirectory, Name, onimFile.Name + ".onim");

            if (!File.Exists(onimPath))
            {
                return false;
            }

            string onimName = targetName.ToLowerInvariant();

            if (onimFile.Name == onimName)
            {
                return true;
            }

            // If there's any animation with the same name, don't.
            bool nameTaken = OnimFiles.Exists(_ => _.Name == onimName);

            if (nameTaken)
            {
                return false;
            }

            string newPath = Path.Combine(FileDirectory, Name, onimName + ".onim");

            if (File.Exists(newPath))
            {
                File.Delete(newPath);
            }

            try
            {
                File.Move(onimPath, newPath);

                onimFile.Name = onimName;

                // Updating paths
                _ = OnimPaths.Remove(onimPath);
                OnimPaths.Add(newPath);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ReloadOnim(OnimFile onimFile)
        {
            string onimPath = Path.Combine(FileDirectory, Name, onimFile.Name + ".onim");
            onimFile.Load(onimPath);
        }

        public bool AddOnim(string targetPath, string targetName = "")
        {
            if (!File.Exists(targetPath))
            {
                return false;
            }

            string onimName = (targetName == "") ? Path.GetFileNameWithoutExtension(targetPath) : targetName;
            string basePath = Path.Combine(FileDirectory, Name);

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            string destOnimPath = Path.Combine(basePath, onimName + ".onim");
            int attempts = 0;

            while (File.Exists(destOnimPath) && attempts < 50)
            {
                attempts++;
                int random = new Random().Next(1000, 10000);
                destOnimPath = Path.Combine(basePath, $"{onimName}_{random}.onim");
            }

            if (attempts >= 50)
            {
                throw new Exception("Failed to generate file name.");
            }

            File.Copy(targetPath, destOnimPath);

            OnimFile onimFile = new OnimFile();
            onimFile.Load(destOnimPath);

            OnimFiles.Add(onimFile);
            OnimPaths.Add(destOnimPath);

            return true;
        }

        public bool ExportOnim(OnimFile onimFile, string targetPath)
        {
            string onimName = onimFile.Name + ".onim";
            string targetFile = Path.Combine(targetPath, onimName);
            string sourceFile = Path.Combine(FileDirectory, Name, onimName);

            try
            {
                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }

                File.Copy(sourceFile, targetFile);

                return true;
            }
            catch { }

            return false;
        }

        public bool ImportOnim(OnimFile previousOnim, string targetPath)
        {
            RemoveOnim(previousOnim);
            return AddOnim(targetPath, previousOnim.Name);
        }

        public void RemoveAllOnims()
        {
            foreach (string onimPath in OnimPaths)
            {
                if (File.Exists(onimPath))
                {
                    File.Delete(onimPath);
                }
            }

            OnimFiles.Clear();
            OnimPaths.Clear();
        }

        /// <summary>
        /// Export animation dictionary names as .nametable file.
        /// </summary>
        /// <param name="filePath">Export path</param>
        public void ExportNames(string filePath)
        {
            if (OnimFiles.Count == 0)
            {
                return;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            Stream content = File.Open(filePath, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(content);

            // Write all animation names since they're stored hashed.
            foreach (OnimFile onim in OnimFiles)
            {
                // Add null terminator to name.
                string name = onim.Name + char.MinValue;
                byte[] buffer = Encoding.ASCII.GetBytes(name);
                writer.Write(buffer);
            }

            writer.Close();
            content.Close();
        }

        /// <summary>
        /// Builds a YCD.
        /// If clips data is not passed, clips will be generated for each animation.
        /// </summary>
        /// <param name="assetClips">Clips data</param>
        /// <returns></returns>
        public byte[] BuildYcd(List<AssetClip> assetClips = null)
        {
            YcdFile ycd = new YcdFile
            {
                ClipDictionary = new ClipDictionary()
            };

            var clipMap = new List<ClipMapEntry>();
            var animMap = new List<AnimationMapEntry>();

            bool generateClips = false;

            if (assetClips == null)
            {
                assetClips = new List<AssetClip>();
                generateClips = true;
            }

            foreach (OnimFile onim in OnimFiles)
            {
                if (!uint.TryParse(onim.Name, out uint onimHash))
                {
                    onimHash = JenkHash.GenHash(onim.Name.ToLowerInvariant());
                }

                Animation anim = new Animation
                {
                    Hash = onimHash,
                    Frames = (ushort)onim.Frames,
                    SequenceFrameLimit = (ushort)onim.SequenceFrameLimit,
                    Duration = onim.Duration
                };

                // Just to make it nicer to debug really
                JenkIndex.Ensure(onim.Name.ToLowerInvariant());

                // TODO: UV animations support

                //bool isUV = false;
                bool hasRootMotion = false;
                var boneIds = new List<AnimationBoneId>();
                //var indexPerBoneId = new Dictionary<uint, int>();
                var seqs = new List<Sequence>();
                var aseqs = new List<List<AnimSequence>>();

                foreach (var oseq in onim.SequenceList)
                {
                    var boneid = new AnimationBoneId();
                    boneid.BoneId = (ushort)oseq.BoneID;
                    // boneid.Unk0 = 0; // what to use here?

                    // TODO: figure out unknown tracks.

                    switch (oseq.Track)
                    {
                        case "BonePosition":
                            boneid.Track = 0;
                            break;
                        case "BoneRotation":
                            boneid.Track = 1;
                            break;
                        case "ModelPosition":
                            boneid.Track = 5; hasRootMotion = true;
                            break;
                        case "ModelRotation":
                            boneid.Track = 6; hasRootMotion = true;
                            break;
                        case "UV0":
                            boneid.Track = 17; // isUV = true;
                            break;
                        case "UV1":
                            boneid.Track = 18; // isUV = true;
                            break;
                        case "LightColor":
                            boneid.Track = 0; // what should this be?
                            break;
                        case "LightRange":
                            boneid.Track = 0; // what should this be?
                            break;
                        case "LightIntensity1":
                            boneid.Track = 0; // what should this be?
                            break;
                        case "LightIntensity2":
                            boneid.Track = 0; // what should this be?
                            break;
                        case "LightDirection":
                            boneid.Track = 0; // what should this be?
                            break;
                        case "Type21":
                            boneid.Track = 0; // what should this be?
                            break;
                        case "CameraPosition":
                            boneid.Track = 7;
                            break;
                        case "CameraRotation":
                            boneid.Track = 8;
                            break;
                        case "CameraFOV":
                            boneid.Track = 0; // what should this be?
                            break;
                        case "CameraDof":
                            boneid.Track = 0; // what should this be?
                            break;
                        case "CameraMatrixRotateFactor":
                            boneid.Track = 0; // what should this be?
                            break;
                        case "CameraControl":
                            boneid.Track = 0; // what should this be?
                            break;
                        case "ActionFlags": // not sure what this is for? just ignore it for now
                            continue;
                        default:
                            break;
                    }

                    boneid.Unk0 = boneid.Track;

                    /*
                    uint idx = (uint)((boneid.Track << 16) | boneid.BoneId);
                    if (!indexPerBoneId.ContainsKey(idx))
					{
                        indexPerBoneId[idx] = 0;
					}

                    boneid.Unk0 = (byte)indexPerBoneId[idx];
                    indexPerBoneId[idx]++;
                    */

                    boneIds.Add(boneid);

                    for (int i = 0; i < oseq.FramesData.Count; i++)
                    {
                        var framesData = oseq.FramesData[i];

                        Sequence seq = null;
                        List<AnimSequence> aseqlist = null;
                        while (i >= seqs.Count)
                        {
                            seq = new Sequence();
                            seqs.Add(seq);
                            aseqlist = new List<AnimSequence>();
                            aseqs.Add(aseqlist);
                        }
                        seq = seqs[i];
                        aseqlist = aseqs[i];


                        var channelList = new List<AnimChannel>();

                        if (framesData.IsStatic)
                        {
                            float[] vals = (framesData.Channels.Count > 0) ? framesData.Channels[0].Values : null;

                            if (vals != null)
                            {
                                if (vals.Length == 1)
                                {
                                    var acsf = new AnimChannelStaticFloat();
                                    acsf.Value = vals[0];
                                    channelList.Add(acsf);
                                }
                                else if (vals.Length == 3)
                                {
                                    var acsv = new AnimChannelStaticVector3();
                                    acsv.Value = new Vector3(vals[0], vals[1], vals[2]);
                                    channelList.Add(acsv);
                                }
                                else if (vals.Length == 4)
                                {
                                    var acsq = new AnimChannelStaticQuaternion();
                                    acsq.Value = new Quaternion(vals[0], vals[1], vals[2], vals[3]);
                                    channelList.Add(acsq);
                                }
                            }
                        }
                        else
                        {
                            int channelsCount = framesData.Channels.Count;

                            for (int c = 0; c < channelsCount; c++)
                            {
                                var ochan = framesData.Channels[c];
                                var vals = ochan.Values;
                                if (vals.Length == 1)//static channel...
                                {
                                    var acsf = new AnimChannelStaticFloat();
                                    acsf.Value = vals[0];
                                    channelList.Add(acsf);
                                }
                                else //if (vals.Length == onim.Frames)
                                {
                                    float minval = float.MaxValue;
                                    float maxval = float.MinValue;
                                    float lastval = 0;
                                    float mindelta = float.MaxValue;

                                    foreach (var val in vals)
                                    {
                                        minval = Math.Min(minval, val);
                                        maxval = Math.Max(maxval, val);

                                        if (val != lastval)
                                        {
                                            float adelta = Math.Abs(val - lastval);
                                            mindelta = Math.Min(mindelta, adelta);
                                        }

                                        lastval = val;
                                    }

                                    if (mindelta == float.MaxValue) mindelta = 0;

                                    float range = maxval - minval;
                                    float minquant = range / 1048576.0f;
                                    float quantum = Math.Max(mindelta, minquant);

                                    var acqf = new AnimChannelQuantizeFloat();
                                    acqf.Values = vals;
                                    acqf.Offset = minval;
                                    acqf.Quantum = quantum;
                                    channelList.Add(acqf);
                                }
                            }

                            if (channelsCount == 4)
                            {
                                // Assume it's a quaternion... add the extra quaternion channel
                                var acq1 = new AnimChannelCachedQuaternion(AnimChannelType.CachedQuaternion1);
                                acq1.QuatIndex = 3; // What else should it be?
                                channelList.Add(acq1);
                            }
                        }

                        if (channelList.Count == 4)
                        { }//shouldn't happen

                        AnimSequence aseq = new AnimSequence
                        {
                            Channels = channelList.ToArray()
                        };

                        aseqlist.Add(aseq);

                    }
                }

                int remframes = anim.Frames;

                for (int i = 0; i < seqs.Count; i++)
                {
                    var seq = seqs[i];
                    var aseqlist = aseqs[i];

                    //seq.Unknown_00h = 0; // what to set this???
                    seq.Unknown_00h = new MetaHash((uint)i); // should be data hash of actual data but this is built waaay later
                    seq.NumFrames = (ushort)Math.Max(Math.Min(anim.SequenceFrameLimit + 1, remframes), 0);
                    seq.Sequences = aseqlist.ToArray();

                    seq.AssociateSequenceChannels();

                    remframes -= anim.SequenceFrameLimit + 1;
                }

                anim.BoneIds = new ResourceSimpleList64_s<AnimationBoneId>
                {
                    data_items = boneIds.ToArray()
                };

                anim.Sequences = new ResourcePointerList64<Sequence>
                {
                    data_items = seqs.ToArray()
                };

                anim.Unknown_10h = hasRootMotion ? (byte)16 : (byte)0;
                anim.Unknown_1Ch = 0; // ???

                anim.AssignSequenceBoneIds();


                // TODO: UV animations support
                /*
                MetaHash clipHash = anim.Hash;
                if (isUV)
                {
                    var name = onim.Name.ToLowerInvariant();
                    var uvind = name.IndexOf("_uv_");
                    if (uvind < 0)
                    { }
                    var modelname = name.Substring(0, uvind);
                    var geoindstr = name.Substring(uvind + 4);
                    var geoind = 0u;
                    uint.TryParse(geoindstr, out geoind);
                    clipHash = JenkHash.GenHash(modelname) + geoind + 1;
                }
                */

                if (generateClips)
                {
                    AssetClipAnimation clipAnimation = new AssetClipAnimation()
                    {
                        Name = onim.Name,
                        Rate = 1.0f,
                        StartTime = 0.0f,
                        EndTime = onim.Duration,
                    };

                    AssetClip assetClip = new AssetClip()
                    {
                        Name = onim.Name,
                        Duration = onim.Duration,
                        Animations = new List<AssetClipAnimation>()
                        {
                            clipAnimation
                        }
                    };

                    assetClips.Add(assetClip);
                }

                var ame = new AnimationMapEntry
                {
                    Hash = anim.Hash, // is this right? what else to use?
                    Animation = anim
                };

                animMap.Add(ame);
            }

            // TODO: verify that there's no weird code.

            foreach (AssetClip clipItem in assetClips)
            {
                int animationsCount = clipItem.Animations.Count;

                // No animations? Skip.
                if (animationsCount == 0)
                {
                    continue;
                }

                if (animationsCount > 1)
                {
                    // If clip uses multiple animations, create array.
                    var clipsArray = new ResourceSimpleArray<ClipAnimationsEntry>();

                    // Iterate over all associated animations.
                    foreach (var clipEntry in clipItem.Animations)
                    {
                        // Get hash of clip associated animation name.
                        if (!uint.TryParse(clipEntry.Name, out uint animHash))
                        {
                            animHash = JenkHash.GenHash(clipEntry.Name);
                        }

                        // Find entry of associated animation in animations map by hash.
                        AnimationMapEntry animEntry = animMap.Find(_ => _.Hash == animHash);

                        // Entry not found? That's weird, skip to next animation entry.
                        if (animEntry == null)
                        {
                            continue;
                        }

                        ClipAnimationsEntry clip = new ClipAnimationsEntry
                        {
                            Animation = animEntry.Animation,
                            StartTime = clipEntry.StartTime,
                            EndTime = clipEntry.EndTime,
                            Unknown_0Ch = 0
                        };

                        clipsArray.Add(clip);
                    }

                    // Now get hash of clip name.
                    if (!uint.TryParse(clipItem.Name, out uint clipHash))
                    {
                        clipHash = JenkHash.GenHash(clipItem.Name);
                    }

                    ClipAnimationList clipList = new ClipAnimationList
                    {
                        Hash = clipHash,
                        Name = "pack:/" + clipItem.Name + ".clip", // pack:/name.clip
                        Unknown_30h = 0, // what's this then?
                        Properties = new ClipPropertyMap(),
                        Animations = clipsArray,
                    };

                    ClipMapEntry cme = new ClipMapEntry
                    {
                        Clip = clipList,
                        Hash = clipHash
                    };

                    clipMap.Add(cme);
                }
                else
                {
                    // If it's a single animation clip, just get first.
                    AssetClipAnimation clipEntry = clipItem.Animations.First();

                    // Get hash of clip associated animation name.
                    if (!uint.TryParse(clipEntry.Name, out uint animHash))
                    {
                        animHash = JenkHash.GenHash(clipEntry.Name);
                    }

                    // Find entry of associated animation in animations map by hash.
                    AnimationMapEntry animEntry = animMap.Find(_ => _.Hash == animHash);

                    // Entry not found? That's weird, skip to next clip.
                    if (animEntry == null)
                    {
                        continue;
                    }

                    // Now get hash of clip name.
                    if (!uint.TryParse(clipItem.Name, out uint clipHash))
                    {
                        clipHash = JenkHash.GenHash(clipItem.Name);
                    }

                    ClipAnimation clip = new ClipAnimation
                    {
                        Hash = clipHash,
                        Animation = animEntry.Animation,
                        StartTime = clipEntry.StartTime,
                        EndTime = clipEntry.EndTime,
                        Rate = clipEntry.Rate,
                        Name = "pack:/" + clipItem.Name + ".clip", // pack:/name.clip
                        Unknown_30h = 0, // what's this then?
                        Properties = new ClipPropertyMap()
                    };

                    clip.Properties.CreatePropertyMap(null); // TODO: properties support
                    clip.Tags = new ClipTagList(); // TODO: tags support

                    ClipMapEntry cme = new ClipMapEntry
                    {
                        Clip = clip,
                        Hash = clipHash
                    };

                    clipMap.Add(cme);
                }
            }

            ycd.ClipDictionary.CreateClipsMap(clipMap.ToArray());
            ycd.ClipDictionary.CreateAnimationsMap(animMap.ToArray());

            ycd.ClipDictionary.BuildMaps();
            ycd.ClipDictionary.UpdateUsageCounts();
            ycd.InitDictionaries();

            // TODO: this code is extremely stupid, but actually very important for #CDs somehow
            // probably CodeWalker doing some magic when resaving. Maybe our code is a bit outdated.
            // But anyway, without resaving even default animations will be broken. Need to fix this one day.

            byte[] ycdRaw = ycd.Save();
            YcdFile ycdOut = new YcdFile();
            RpfFile.LoadResourceFile(ycdOut, ycdRaw, 46);

            byte[] data = ycdOut.Save();
            return data;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
