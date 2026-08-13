using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using UnityEditor;

namespace OpenToolkit.HierarchyIcons
{
    // Tree view members are resolved from the runtime instance rather than from named types:
    // Unity 6.5 made the whole tree view generic (TreeViewController<EntityId>) and left the
    // old non-generic names behind as unrelated deprecated shims that no instance ever matches.
    // Tree items are passed around as object for the same reason - they are only ever used as
    // opaque handles for identity and for feeding back into the controller.
    public static class ReflectionHelper
    {
        static BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        static Type SceneHierarchyWindowType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");

        static MethodInfo s_isRenamingMethod;
        static PropertyInfo s_stateRenameOverlayProperty;
        static PropertyInfo s_stateProperty;
        public static bool IsTreeRenaming(object treeController)
        {
            if (s_stateProperty == null)
            {
                s_stateProperty = treeController.GetType().GetProperty("state", FLAGS);
            }

            var state = s_stateProperty.GetValue(treeController);

            if (state == null)
            {
                return false;
            }

            if (s_stateRenameOverlayProperty == null)
            {
                s_stateRenameOverlayProperty = state.GetType().GetProperty("renameOverlay", FLAGS);
            }

            var renameOverlay = s_stateRenameOverlayProperty.GetValue(state);

            if (renameOverlay == null)
            {
                return false;
            }

            if (s_isRenamingMethod == null)
            {
                s_isRenamingMethod = renameOverlay.GetType().GetMethod("IsRenaming");
            }

            bool? isRenaming = s_isRenamingMethod.Invoke(renameOverlay, null) as bool?;

            return isRenaming == true;
        }

        public static object GetTreeController(EditorWindow hierarchyWindow)
        {
            if (hierarchyWindow == null)
            {
                return null;
            }

            if (hierarchyWindow.GetType().Name != "SceneHierarchyWindow")
            {
                return null;
            }

            var prop = hierarchyWindow.GetType().GetProperty("sceneHierarchy", FLAGS);
            var sceneHierarchy = prop?.GetValue(hierarchyWindow);

            if (sceneHierarchy == null)
            {
                return null;
            }

            var treeViewProperty = sceneHierarchy.GetType().GetProperty("treeView", FLAGS);

            return treeViewProperty?.GetValue(sceneHierarchy);
        }

        static PropertyInfo s_dataProperty;
        static object GetData(object treeController)
        {
            if (s_dataProperty == null)
            {
                s_dataProperty = treeController.GetType().GetProperty("data", FLAGS);
            }

            return s_dataProperty.GetValue(treeController);
        }

        static MethodInfo s_getItemMethod;
        public static object GetItem(int row, object treeController)
        {
            var data = GetData(treeController);

            if (GetRowCount(treeController) <= row)
            {
                return null;
            }

            if (s_getItemMethod == null)
            {
                s_getItemMethod = data.GetType().GetMethod("GetItem", FLAGS);
            }

            return s_getItemMethod.Invoke(data, new object[] { row });
        }

        static MethodInfo s_getRowMethod;
        public static int GetRow(EntityId id, object treeController)
        {
            var data = GetData(treeController);

            if (s_getRowMethod == null)
            {
                s_getRowMethod = data.GetType().GetMethod("GetRow", FLAGS);
            }

            return (int)s_getRowMethod.Invoke(data, new object[] { id });
        }

        static PropertyInfo s_getRowCountProperty;
        public static int GetRowCount(object treeController)
        {
            var data = GetData(treeController);

            if (s_getRowCountProperty == null)
            {
                s_getRowCountProperty = data.GetType().GetProperty("rowCount", FLAGS);
            }

            return (int)s_getRowCountProperty.GetValue(data);
        }

        static MethodInfo s_isExpandedMethod;
        public static bool IsExpanded(EntityId id, object treeController)
        {
            var data = GetData(treeController);

            if (s_isExpandedMethod == null)
            {
                s_isExpandedMethod = data.GetType().GetMethod("IsExpanded", new Type[] { typeof(EntityId) });
            }

            return (bool)s_isExpandedMethod.Invoke(data, new object[] { id });
        }

        static MethodInfo s_isItemDragSelectedOrSelectedMethod;
        public static bool IsItemDragSelectedOrSelected(object item, object treeController)
        {
            if (s_isItemDragSelectedOrSelectedMethod == null)
            {
                s_isItemDragSelectedOrSelectedMethod = treeController.GetType().GetMethod("IsItemDragSelectedOrSelected", FLAGS);
            }

            return (bool)s_isItemDragSelectedOrSelectedMethod.Invoke(treeController, new object[] { item });
        }

        static PropertyInfo s_hoverItemProperty;
        public static object GetHoverItem(object treeController)
        {
            if (s_hoverItemProperty == null)
            {
                s_hoverItemProperty = treeController.GetType().GetProperty("hoveredItem", FLAGS);
            }

            return s_hoverItemProperty.GetValue(treeController);
        }

        static PropertyInfo s_isDraggingProperty;
        static PropertyInfo s_draggingProperty;
        static MethodInfo s_getDropTargetControlIDMethod;
        public static bool IsDragging(object treeController)
        {
            if (s_isDraggingProperty == null)
            {
                s_isDraggingProperty = treeController.GetType().GetProperty("isDragging", FLAGS);
            }

            if (s_draggingProperty == null)
            {
                s_draggingProperty = treeController.GetType().GetProperty("dragging", FLAGS);
            }

            var dragging = s_draggingProperty.GetValue(treeController);

            if (dragging == null)
            {
                return false;
            }

            if (s_getDropTargetControlIDMethod == null)
            {
                s_getDropTargetControlIDMethod = dragging.GetType().GetMethod("GetDropTargetControlID", FLAGS);
            }

            bool isDragging = (bool)s_isDraggingProperty.GetValue(treeController);
            int dropTargetId = (int)s_getDropTargetControlIDMethod.Invoke(dragging, new object[] { });

            return isDragging && dropTargetId == 0;
        }

        static MethodInfo s_getSceneHierarchiesMethod;

        public static List<SearchableEditorWindow> GetAllSceneHierarchyWindows()
        {
            if (s_getSceneHierarchiesMethod == null)
            {
                s_getSceneHierarchiesMethod = SceneHierarchyWindowType.GetMethod("GetAllSceneHierarchyWindows", BindingFlags.Public | BindingFlags.Static);
            }

            var list = (IList)s_getSceneHierarchiesMethod.Invoke(null, null);

            List<SearchableEditorWindow> windows = new List<SearchableEditorWindow>();

            foreach (var window in list)
            {
                windows.Add(window as SearchableEditorWindow);
            }

            return windows;
        }

        static PropertyInfo s_hasSearchFilterInfo;

        public static bool HasSearchFilter(SearchableEditorWindow searchableEditorWindow)
        {
            if (s_hasSearchFilterInfo == null)
            {
                s_hasSearchFilterInfo = typeof(SearchableEditorWindow).GetProperty("hasSearchFilter", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            bool? hasSearchFilter = s_hasSearchFilterInfo.GetValue(searchableEditorWindow) as bool?;

            return hasSearchFilter == true;
        }

        static PropertyInfo s_hasSearchFilterFocusInfo;
        public static bool HasSearchFilterFocus(SearchableEditorWindow searchableEditorWindow)
        {
            if (s_hasSearchFilterFocusInfo == null)
            {
                s_hasSearchFilterFocusInfo = typeof(SearchableEditorWindow).GetProperty("hasSearchFilterFocus", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            bool? hasSearchFilterFocus = s_hasSearchFilterFocusInfo.GetValue(searchableEditorWindow) as bool?;

            return hasSearchFilterFocus == true;
        }
    }
}
