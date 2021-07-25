﻿using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using Reqtificator.Configuration;
using Reqtificator.StaticReferences;
using Serilog;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Reqtificator.Transformers.EncounterZones
{
    internal class OpenCombatBoundaries : Transformer<EncounterZone, IEncounterZone, IEncounterZoneGetter>
    {
        private readonly IReadOnlySet<IFormLink<IEncounterZoneGetter>> _alwaysClosedZones;
        private readonly bool _openCombatZones;

        internal OpenCombatBoundaries(IReadOnlySet<IFormLink<IEncounterZoneGetter>> alwaysClosedZones,
            bool openCombatZones)
        {
            _alwaysClosedZones = alwaysClosedZones;
            _openCombatZones = openCombatZones;
        }

        public OpenCombatBoundaries(ILoadOrder<IModListing<ISkyrimModGetter>> loadOrder, UserSettings userConfig)
        {
            var linkCache = loadOrder.ToImmutableLinkCache();
            var exclusions = ImmutableHashSet<IFormLink<IEncounterZoneGetter>>.Empty;
            foreach (var recordVersion in FormLists.ClosedEncounterZones.ResolveAll(linkCache))
            {
                foreach (var entry in recordVersion.Items)
                {
                    if (entry.TryResolve<IEncounterZoneGetter>(linkCache, out var zone))
                    {
                        exclusions = exclusions.Add(zone.AsLink());
                    }
                    else
                    {
                        //TODO: error handling strategy: throw or log? fail fast or collect and aggregate as events?
                    }
                }
            }

            _alwaysClosedZones = exclusions;
            _openCombatZones = userConfig.OpenEncounterZones;
        }

        public override bool ShouldProcess(IEncounterZoneGetter record)
        {
            return _openCombatZones && !_alwaysClosedZones.Contains(new FormLink<IEncounterZoneGetter>(record));
        }

        public override void Process(IEncounterZone record)
        {
            record.Flags |= EncounterZone.Flag.DisableCombatBoundary;
            Log.Debug("opened combat boundaries of encounter zone");
        }
    }
}