﻿using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace Reqtificator.Export
{
    internal class PatchData
    {
        public static void SetPatchHeadersAndVersion(ISkyrimModGetter requiem, ISkyrimMod patch, RequiemVersion version)
        {
            patch.ModHeader.Author = "The Requiem Dungeon Masters";

            patch.ModHeader.Description =
                "This is an autogenerated patch for the mod \"Requiem - The Role-Playing Overhaul\". " +
                $"Generated for Requiem version: {version.ShortVersion()} -- build with Mutagen";

            //TODO: error handling...
            if (requiem.Globals.RecordCache.TryGetValue(StaticReferences.GlobalVariables.VersionStamp, out var original))
            {
                var record = patch.Globals.GetOrAddAsOverride(original);
                record.RawFloat = version.AsNumeric();
            }
        }
    }
}