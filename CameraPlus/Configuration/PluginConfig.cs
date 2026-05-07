using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace CameraPlus.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        //public virtual int IntValue { get; set; } = 42; // Must be 'virtual' if you want BSIPA to detect a value change and save the config automatically.

        [NonNullable]
        public virtual bool ProfileSceneChange { get; set; } = false;
        [NonNullable]
        public virtual string MenuProfile { get; set; } = string.Empty;
        [NonNullable]
        public virtual string GameProfile { get; set; } = string.Empty;
        [NonNullable]
        public virtual string RotateProfile { get; set; } = string.Empty;
        [NonNullable]
        public virtual string MultiplayerProfile { get; set; } = string.Empty;
        [NonNullable]
        public virtual string SongSpecificScriptProfile { get; set; } = string.Empty;
        [NonNullable]
        public virtual string CameraQuadPosition { get; set; } = string.Empty;
        [NonNullable]
        public virtual float CameraCubeSize { get; set; } = 1.0f;
        [NonNullable]
        public virtual bool CameraQuadStretch { get; set; } = true;
        
        [NonNullable]
        public virtual float SpoutCameraAlpha { get; set; } = 0.99f;
        
        [NonNullable]
        public virtual bool AvatarSpoutAutoConnect { get; set; } = false;
        [NonNullable]
        public virtual string AvatarSpoutName { get; set; } = string.Empty;
        [NonNullable]
        public virtual float AvatarSpoutPositionOffsetX { get; set; } = 0.0f;
        [NonNullable]
        public virtual float AvatarSpoutPositionOffsetY { get; set; } = 0.0f;
        [NonNullable]
        public virtual bool AvatarSpoutInMenu { get; set; } = false;
        [NonNullable]
        public virtual bool AvatarSpoutInGame { get; set; } = false;
        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload()
        {
            // Do stuff after config is read from disk.
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(PluginConfig other)
        {
            // This instance's members populated from other
        }
    }
}
