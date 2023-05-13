using System;
using System.Collections.Generic;
using System.IO;
using YH.AssetManage;

namespace AssetBundleBuilder
{
    public class AssetBundleManifestWriter : AssetBundleManifestSerialzer
    {
        protected BinaryWriter _Writer;

        List<AssetBundleManifestBlockInfo> _BlockInfos;
        protected int _BlockIndex = 0;
        protected long _BlockTableOffset = 0;

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

            AddBlockInfo(BlockType.Version);
            AddBlockInfo(BlockType.Bundle);

            SetBlockTableCount();

            WriteHeader();

            //block table
            StoreBlockTableOffset();

            WriteVersion(version);

            WriteBundles(bundles);

            RestoreBlockTableOffset();

            WriteBlockTable();
        }

        protected void CreateHeader(ManifestFlag flag)
        {
            _Header.magic = Magic;
            _Header.format = Format;
            _Header.flag = flag;
            _Header.blockCount = 0;
        }

        protected void WriteHeader()
        {
            _Writer.Write(_Header.magic);
            _Writer.Write(_Header.format);
            _Writer.Write((ushort)_Header.flag);
            _Writer.Write(_Header.blockCount);
        }

        protected void SetBlockTableCount()
        {
            _Header.blockCount =(byte)_BlockInfos.Count;
        }

        protected int GetBlockTableRawSize()
        {
            return sizeof(int) * _BlockInfos.Count;
        }

        protected int AddBlockInfo(BlockType type)
        {
            if (_BlockInfos == null)
            {
                _BlockInfos = new List<AssetBundleManifestBlockInfo>();
            }
            AssetBundleManifestBlockInfo blockInfo = new AssetBundleManifestBlockInfo();
            blockInfo.type = type;
            _BlockInfos.Add(blockInfo);
            return _BlockInfos.Count - 1;
        }

        protected void SetBlockOffset(BlockType type, uint offset)
        {
            for (int i = 0; i < _BlockInfos.Count; ++i)
            {
                if (_BlockInfos[i].type == type)
                {
                    SetBlockOffset(i, offset);
                    return;
                }
            }
        }

        protected void SetBlockOffset(int blockIndex, uint offset)
        {
            if (blockIndex >= 0 && blockIndex < _BlockInfos.Count)
            {
                AssetBundleManifestBlockInfo blockInfo = _BlockInfos[blockIndex];
                blockInfo.offset = offset;
                _BlockInfos[blockIndex] = blockInfo;
            }
        }

        protected void StoreBlockTableOffset()
        {
            _BlockTableOffset = _Writer.BaseStream.Position;
            _Writer.BaseStream.Position += GetBlockTableRawSize();
        }

        protected void RestoreBlockTableOffset()
        {
            _Writer.BaseStream.Position = _BlockTableOffset;
        }

        protected void WriteBlockTable()
        {
            for (int i = 0; i < _BlockInfos.Count; ++i)
            {
                _Writer.Write(_BlockInfos[i].SerializeValue());
            }
        }

        protected void WriteVersion(Version version)
        {
            VersionSerializer.SerializeVersion(version, _Writer);
            SetBlockOffset(BlockType.Version, (uint)_Writer.BaseStream.Position);
        }

        protected void WriteBundles(List<BundleInfo> bundles)
        {
            if(bundles==null || bundles.Count == 0)
            {
                return;
            }
            
            _Writer.Write(bundles.Count);

            //write base info
            for (int i = 0; i < bundles.Count; i++)
            {
                BundleInfo bundleInfo = bundles[i];
                bundleInfo.serializeIndex = i;
                _Writer.Write(bundleInfo.contentHash);
                WriteAssets(bundleInfo);
            }

            //write deps
            for (int i = 0; i < bundles.Count; i++)
            {
                BundleInfo bundleInfo = bundles[i];
                WriteDependencies(bundleInfo);
            }

            SetBlockOffset(BlockType.Bundle, (uint)_Writer.BaseStream.Position);
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
                ulong hashCode = YH.Hash.xxHash.xxHash64.ComputeHash(asset.assetPath);
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
                _Writer.Write(dep.serializeIndex);
            }
        }
    }
}
