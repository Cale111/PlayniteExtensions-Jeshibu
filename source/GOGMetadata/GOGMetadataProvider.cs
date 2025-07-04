﻿using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace GOGMetadata;

public class GOGMetadataProvider : GenericMetadataProvider<GogSearchResponse.Product>
{
    public GOGMetadataProvider(IGameSearchProvider<GogSearchResponse.Product> dataSource, MetadataRequestOptions options, IPlayniteAPI playniteApi, IPlatformUtility platformUtility) : base(dataSource, options, playniteApi, platformUtility)
    {
    }

    public override List<MetadataField> AvailableFields => Fields;

    public static List<MetadataField> Fields = new List<MetadataField> {
        MetadataField.Name,
        MetadataField.Description,
        MetadataField.Features,
        MetadataField.Genres,
        MetadataField.Tags,
        MetadataField.Platform,
        MetadataField.ReleaseDate,
        MetadataField.Developers,
        MetadataField.Publishers,
        MetadataField.InstallSize,
        MetadataField.BackgroundImage,
        MetadataField.CoverImage,
        MetadataField.Icon,
        MetadataField.Links,
        MetadataField.CommunityScore,
    };

    protected override string ProviderName { get; } = "GOG";
}