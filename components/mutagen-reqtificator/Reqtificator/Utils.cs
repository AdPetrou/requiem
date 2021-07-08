﻿using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Hocon;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Reqtificator.Exceptions;
using Reqtificator.Transformers.Rules;

namespace Reqtificator
{
    internal static class Utils
    {
        public static ErrorOr<IImmutableSet<IFormLinkGetter<TGetter>>> GetRecordsFromAllImports<TGetter>(
            IFormLinkGetter<IFormListGetter> formListLink,
            ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache) where TGetter : class, ISkyrimMajorRecordGetter
        {
            IImmutableSet<IFormLinkGetter<TGetter>> records = ImmutableHashSet<IFormLinkGetter<TGetter>>.Empty;
            foreach (var recordVersion in formListLink.ResolveAll(linkCache))
            {
                foreach (var entry in recordVersion.Items)
                {
                    if (entry.TryResolve<TGetter>(linkCache, out var resolved))
                    {
                        records = records.Add(resolved.AsLink());
                    }
                    else
                    {
                        var error = new InvalidRecordReferenceException<TGetter>(new FormLink<TGetter>(entry.FormKey));
                        return new Failed<IImmutableSet<IFormLinkGetter<TGetter>>>(error);
                    }
                }
            }

            return records.AsSuccess();
        }

        public static ErrorOr<IImmutableList<Config>> LoadModConfigFiles(GameContext context, string filePrefix)
        {
            //TODO: graceful error handling for not parseable configuration files
            var armorConfigPath = Path.Combine(context.DataFolder, "SkyProc Patchers", "Requiem", "Data");
            IImmutableList<Config> configs = context.ActiveMods
                .Select(m => Path.Combine(armorConfigPath, $"{filePrefix}_{m.ModKey.FileName}.conf"))
                .Where(f => File.Exists(f))
                .Select(f => HoconConfigurationFactory.FromFile(f))
                .ToImmutableList();
            return configs.AsSuccess();
        }
    }
}