﻿using System;
using UnityEngine;

namespace KBCore.Refs {
    /// <summary>
    /// RefLoc indicates the expected location of the reference.
    /// </summary>
    internal enum RefLoc {
        /// <summary>
        /// Anywhere will only validate the reference isn't null, but relies on you to 
        /// manually assign the reference yourself.
        /// </summary>
        Anywhere = -1,
        /// <summary>
        /// Self looks for the reference on the same game object as the attributed component
        /// using GetComponent(s)()
        /// </summary>
        Self = 0,
        /// <summary>
        /// Parent looks for the reference on the parent hierarchy of the attributed components game object
        /// using GetComponent(s)InParent()
        /// </summary>
        Parent = 1,
        /// <summary>
        /// Child looks for the reference on the child hierarchy of the attributed components game object
        /// using GetComponent(s)InChildren()
        /// </summary>
        Child = 2,
        /// <summary>
        /// Scene looks for the reference anywhere in the scene
        /// using GameObject.FindAnyObjectByType() and GameObject.FindObjectsOfType()
        /// </summary>
        Scene = 4,
    }

    /// <summary>
    /// Optional flags offering additional functionality.
    /// </summary>
    [Flags]
    public enum Flag {
        /// <summary>
        /// Default behaviour.
        /// </summary>
        None = 0,
        /// <summary>
        /// Allow empty (or null in the case of non-array types) results.
        /// </summary>
        Optional = 1,
        /// <summary>
        /// Include inactive components in the results (only applies to Child, Scene, and Parent). 
        /// </summary>
        IncludeInactive = 2,
        /// <summary>
        /// Allow the field to be editable in the inspector
        /// </summary>
        Editable = 4,
        /// <summary>
        /// Don't display the field in the inspector. Doesn't hide the HelpBox
        /// </summary>
        Hidden = 8,
    }

    /// <summary>
    /// Attribute allowing you to decorate component reference fields with their search criteria. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class SceneRefAttribute : PropertyAttribute {
        internal RefLoc Loc { get; }
        internal Flag Flags { get; }

        internal SceneRefAttribute(RefLoc loc, Flag flags = Flag.None) {
            this.Loc = loc;
            this.Flags = flags;
        }

        internal bool HasFlags(Flag flags)
            => (this.Flags & flags) == flags;
    }

    /// <summary>
    /// Anywhere will only validate the reference isn't null, but relies on you to 
    /// manually assign the reference yourself.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GetAnywhereAttribute : SceneRefAttribute {
        public GetAnywhereAttribute(Flag flags = Flag.Editable)
            : base(RefLoc.Anywhere, flags: flags) { }
    }

    /// <summary>
    /// GetOnSelf looks for the reference on the same game object as the attributed component
    /// using GetComponent(s)()
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GetOnSelfAttribute : SceneRefAttribute {
        public GetOnSelfAttribute(Flag flags = Flag.None)
            : base(RefLoc.Self, flags: flags) { }
    }

    /// <summary>
    /// GetOnParent looks for the reference on the parent hierarchy of the attributed components game object
    /// using GetComponent(s)InParent()
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GetOnParentAttribute : SceneRefAttribute {
        public GetOnParentAttribute(Flag flags = Flag.None)
            : base(RefLoc.Parent, flags: flags) { }
    }

    /// <summary>
    /// GetOnChild looks for the reference on the child hierarchy of the attributed components game object
    /// using GetComponent(s)InChildren()
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GetOnChildAttribute : SceneRefAttribute {
        public GetOnChildAttribute(Flag flags = Flag.None)
            : base(RefLoc.Child, flags: flags) { }
    }

    /// <summary>
    /// GetInScene looks for the reference anywhere in the scene
    /// using GameObject.FindAnyObjectByType() and GameObject.FindObjectsOfType()
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GetInSceneAttribute : SceneRefAttribute {
        public GetInSceneAttribute(Flag flags = Flag.None)
            : base(RefLoc.Scene, flags: flags) { }
    }
}