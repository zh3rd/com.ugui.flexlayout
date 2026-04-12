using UnityEngine;
using UnityEngine.UI.Flex;

namespace UnityEngine.UI.Flex.Core
{
    internal readonly struct FlexOwnership
    {
        public FlexOwnership(
            bool driveAnchors,
            bool drivePositionX,
            bool drivePositionY,
            bool driveSizeX,
            bool driveSizeY)
        {
            DriveAnchors = driveAnchors;
            DrivePositionX = drivePositionX;
            DrivePositionY = drivePositionY;
            DriveSizeX = driveSizeX;
            DriveSizeY = driveSizeY;
        }

        public bool DriveAnchors { get; }
        public bool DrivePositionX { get; }
        public bool DrivePositionY { get; }
        public bool DriveSizeX { get; }
        public bool DriveSizeY { get; }

        public FlexDriveMask ToDriveMask()
        {
            var mask = FlexDriveMask.None;

            if (DriveAnchors)
            {
                mask |= FlexDriveMask.Anchors;
            }

            if (DrivePositionX)
            {
                mask |= FlexDriveMask.PositionX;
            }

            if (DrivePositionY)
            {
                mask |= FlexDriveMask.PositionY;
            }

            if (DriveSizeX)
            {
                mask |= FlexDriveMask.SizeX;
            }

            if (DriveSizeY)
            {
                mask |= FlexDriveMask.SizeY;
            }

            return mask;
        }
    }

    internal static class FlexOwnershipResolver
    {
        public static bool ShouldDriveChildSize(in ResolvedFlexNode parent, in ResolvedFlexNode child)
        {
            if (child.Node.PositionType == PositionType.Absolute)
            {
                return false;
            }

            if (child.HasNodeSource || child.HasItemSource)
            {
                return true;
            }

            if (child.HasSpecializedContentSource)
            {
                return true;
            }

            if (child.Item.FlexBasis.mode == FlexSizeMode.Points || child.Item.FlexBasis.mode == FlexSizeMode.Percent)
            {
                return true;
            }

            if (child.Node.Width.mode == FlexSizeMode.Points
                || child.Node.Width.mode == FlexSizeMode.Percent
                || child.Node.Height.mode == FlexSizeMode.Points
                || child.Node.Height.mode == FlexSizeMode.Percent)
            {
                return true;
            }

            if (child.Item.FlexGrow > 0f || child.Item.FlexShrink > 0f)
            {
                return true;
            }

            var resolvedAlignSelf = ResolveAlignSelf(parent.Container.AlignItems, child.Item.AlignSelf);
            return resolvedAlignSelf == AlignSelf.Stretch;
        }

        public static FlexOwnership ResolveSelf(bool hasFlexParent, PositionType positionType)
        {
            var drivesSize = !hasFlexParent || positionType == PositionType.Absolute;
            return new FlexOwnership(
                driveAnchors: false,
                drivePositionX: false,
                drivePositionY: false,
                driveSizeX: drivesSize,
                driveSizeY: drivesSize);
        }

        public static FlexOwnership ResolveChild(bool drivesSize, PositionType positionType)
        {
            if (positionType == PositionType.Absolute)
            {
                return new FlexOwnership(
                    driveAnchors: false,
                    drivePositionX: false,
                    drivePositionY: false,
                    driveSizeX: false,
                    driveSizeY: false);
            }

            return new FlexOwnership(
                driveAnchors: true,
                drivePositionX: true,
                drivePositionY: true,
                driveSizeX: drivesSize,
                driveSizeY: drivesSize);
        }

        private static AlignSelf ResolveAlignSelf(AlignItems parentAlignItems, AlignSelf childAlignSelf)
        {
            if (childAlignSelf != AlignSelf.Auto)
            {
                return childAlignSelf;
            }

            return parentAlignItems switch
            {
                AlignItems.Stretch => AlignSelf.Stretch,
                AlignItems.FlexStart => AlignSelf.FlexStart,
                AlignItems.Center => AlignSelf.Center,
                AlignItems.FlexEnd => AlignSelf.FlexEnd,
                _ => AlignSelf.Stretch,
            };
        }
    }
}
