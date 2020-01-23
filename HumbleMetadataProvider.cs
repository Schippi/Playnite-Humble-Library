using Playnite.SDK;
using Playnite.SDK.Metadata;
using Playnite.SDK.Models;

namespace humble
{
    public class HumbleMetadataProvider : LibraryMetadataProvider
    {


    private HumbleGameLibrary library;
        private IPlayniteAPI api;
        public HumbleMetadataProvider(HumbleGameLibrary library,    IPlayniteAPI api){
            this.api = api;
            this.library = library;
        }

        public override GameMetadata GetMetadata(Game game){
            library.metadata.TryGetValue(game.GameId, out var data);
            return data;
        }

    }
}