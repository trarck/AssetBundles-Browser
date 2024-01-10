using System;
using System.Collections.Generic;
using System.IO;
using YH.AssetManage;
using YH.xxHash;

namespace AssetBundleBuilder
{
    public class AssetBundleManifestWriter : AssetBundleManifestSerialzer
    {
        protected BinaryWriter _Writer;

        List<AssetBundleManifestStreamInfo> _StreamInfos;
        protected int _StreamIndex = 0;
        protected long _StreamTableOffset = 0;

        public BinaryWriter Writer
        {
            get { return _Writer; }
        }

        public AssetBundleManifestWriter(Stream stream)
        {
            _Writer = new BinaryWriter(stream);
        }

        public void WriteManifest(Version version, List<BundleInfo> bundles, bool bundleDependenciesAll)
        {
            ManifestFlag flag = ManifestFlag.None;
            
            if (bundleDependenciesAll)
            {
                flag |= ManifestFlag.BundleDependenciesAll;
            }

            CreateHeader(flag);

            AddStreamInfo(StreamType.Version);
            AddStreamInfo(StreamType.Bundle);

            SetStreamTableCount();

            WriteHeader();

            //block table
            StoreStreamTableOffset();

            WriteVersion(version);

            WriteBundles(bundles);

            RestoreStreamTableOffset();

            WriteStreamTable();
        }

        protected void CreateHeader(ManifestFlag flag)
        {
            _Header.magic = Magic;
            _Header.format = CurrentFormat;
            _Header.flag = flag;
            _Header.streamBlockCount = 0;
        }

        protected void WriteHeader()
        {
            _Writer.Write(_Header.magic);
            _Writer.Write(_Header.format);
            _Writer.Write((byte)_Header.flag);
            _Writer.Write(_Header.streamBlockCount);
        }

        protected void SetStreamTableCount()
        {
            _Header.streamBlockCount =(byte)_StreamInfos.Count;
        }

        protected int GetBlockTableRawSize()
        {
            return sizeof(int) * _StreamInfos.Count;
        }

        protected int AddStreamInfo(StreamType type)
        {
            if (_StreamInfos == null)
            {
                _StreamInfos = new List<AssetBundleManifestStreamInfo>();
            }
            AssetBundleManifestStreamInfo blockInfo = new AssetBundleManifestStreamInfo();
            blockInfo.type = type;
            _StreamInfos.Add(blockInfo);
            return _StreamInfos.Count - 1;
        }

        protected void SetBlockOffset(StreamType type, uint offset)
        {
            for (int i = 0; i < _StreamInfos.Count; ++i)
            {
                if (_StreamInfos[i].type == type)
                {
                    SetBlockOffset(i, offset);
                    return;
                }
            }
        }

        protected void SetBlockOffset(int blockIndex, uint offset)
        {
            if (blockIndex >= 0 && blockIndex < _StreamInfos.Count)
            {
                AssetBundleManifestStreamInfo blockInfo = _StreamInfos[blockIndex];
                blockInfo.offset = offset;
                _StreamInfos[blockIndex] = blockInfo;
            }
        }

        protected void StoreStreamTableOffset()
        {
            _StreamTableOffset = _Writer.BaseStream.Position;
            _Writer.BaseStream.Position += GetBlockTableRawSize();
        }

        protected void RestoreStreamTableOffset()
        {
            _Writer.BaseStream.Position = _StreamTableOffset;
        }

        protected void WriteStreamTable()
        {
            for (int i = 0; i < _StreamInfos.Count; ++i)
            {
                _Writer.Write(_StreamInfos[i].SerializeValue());
            }
        }

        protected void WriteVersion(Version version)
        {
            SetBlockOffset(StreamType.Version, (uint)_Writer.BaseStream.Position);
            VersionSerializer.SerializeVersion(version, _Writer);
        }

        protected void WriteBundles(List<BundleInfo> bundles)
        {
            SetBlockOffset(StreamType.Bundle, (uint)_Writer.BaseStream.Position);

            if (bundles==null || bundles.Count == 0)
            {
                return;
            }
            
            // bundle count
            _Writer.Write(bundles.Count);

            //write base info
            for (int i = 0; i < bundles.Count; i++)
            {
                BundleInfo bundleInfo = bundles[i];
                //bundle id
                bundleInfo.bundleId =(ulong)(i+1);
                _Writer.Write(bundleInfo.bundleId);
                WriteAssets(bundleInfo);
            }

            //write deps
            for (int i = 0; i < bundles.Count; i++)
            {
                BundleInfo bundleInfo = bundles[i];
                WriteDependencies(bundleInfo);
            }
        }

        protected void WriteAssets(BundleInfo bundleInfo)
        {
            if (bundleInfo == null || bundleInfo.assets == null || bundleInfo.assets.Count == 0)
            {
                return;
            }

            _Writer.Write(bundleInfo.assets.Count);
            foreach(var asset in bundleInfo.assets)
            {
                ulong hashCode = xxHash64.ComputeHash(asset.assetPath);
                _Writer.Write(hashCode);
            }
        }

        protected void WriteDependencies(BundleInfo bundleInfo)
        {
            if (bundleInfo == null)
            {
                return;
            }

            HashSet<BundleInfo> deps = ((_Header.flag & ManifestFlag.BundleDependenciesAll) == ManifestFlag.BundleDependenciesAll) 
                ? bundleInfo.allDependencies : bundleInfo.dependencies;

            if (deps == null)
            {
                _Writer.Write(0);
                return;
            }

            _Writer.Write(deps.Count);
            foreach (var dep in deps)
            {
                _Writer.Write(dep.bundleId);
            }
        }
    }
}
