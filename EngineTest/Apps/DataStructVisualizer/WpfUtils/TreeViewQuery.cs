﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DataStructVisualizer.WpfUtils
{
    public static class TreeViewQuery
    {
        public static object ItemFromTreeViewItem(TreeViewItem _tvi)
        {
            if (_tvi == null)
                return null;

            TreeViewItem current_tvi = _tvi;
            TreeView tv = null;
            while (current_tvi != null)
            {
                ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(current_tvi);
                if (parent == null)
                    break;

                current_tvi = parent as TreeViewItem;
                tv = parent as TreeView;
            }

            if (tv != null)
            {
                return ItemFromContainer(tv, _tvi);
            }
            return null;
        }

        public static object ItemFromContainer(TreeView _tv, TreeViewItem _tvi)
        {
            object item = _tv.ItemContainerGenerator.ItemFromContainer(_tvi);
            if (item != null && item != DependencyProperty.UnsetValue)
                return item;
            else
                return ItemFromContainer(_tv.ItemContainerGenerator, _tv.Items, _tvi);
        }

        private static object ItemFromContainer(ItemContainerGenerator _parentICG, ItemCollection _items, TreeViewItem _tvi)
        {
            foreach (object curItem in _items)
            {
                TreeViewItem parentTVI = (TreeViewItem)_parentICG.ContainerFromItem(curItem);
                if (parentTVI == null)
                    return null;

                object item = parentTVI.ItemContainerGenerator.ItemFromContainer(_tvi);
                if (item != null && item != DependencyProperty.UnsetValue)
                    return item;

                object recursionResult = ItemFromContainer(parentTVI.ItemContainerGenerator, parentTVI.Items, _tvi);
                if (recursionResult != null)
                    return recursionResult;
            }
            return null;
        }


        public static DependencyObject GetSelectedTreeViewItem(TreeView _tv)
        {
            if (_tv == null)
                return null;

            DependencyObject selObj = null;
            if (_tv.SelectedItem != null)
            {
                selObj = _tv.ItemContainerGenerator.ContainerFromItem(_tv.SelectedItem);
                if (selObj != null)
                    return selObj;
                else
                {
                    var nextTvLevel = _tv.Items;
                    if (nextTvLevel != null && nextTvLevel.Count > 0)
                    {
                        foreach (var item in nextTvLevel)
                        {
                            TreeViewItem tvi = _tv.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                            selObj = GetSelectedTreeViewItem(tvi, _tv.SelectedItem);
                            if (selObj != null)
                                return selObj;
                        }
                    }
                }
            }

            return selObj;
        }

        public static DependencyObject GetSelectedTreeViewItem(TreeViewItem _tvi, object _selItem)
        {
            if (_tvi == null || _selItem == null)
                return null;

            DependencyObject selObj = _tvi.ItemContainerGenerator.ContainerFromItem(_selItem);

            if (selObj == null)
            {
                var nextTvLevel = _tvi.Items;
                if (nextTvLevel != null && nextTvLevel.Count > 0)
                {
                    foreach (var item in nextTvLevel)
                    {
                        TreeViewItem next_tvi = _tvi.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                        selObj = GetSelectedTreeViewItem(next_tvi, _selItem);
                        if (selObj != null)
                            return selObj;
                    }
                }
            }

            return selObj;
        }


        public static TreeViewItem ContainerFromItem(TreeView _tv, object _item)
        {
            if (_tv == null || _item == null)
                return null;

            TreeViewItem containerThatMightContainItem = (TreeViewItem)_tv.ItemContainerGenerator.ContainerFromItem(_item);
            
            if (containerThatMightContainItem != null)
                return containerThatMightContainItem;
            else
                return ContainerFromItem(_tv.ItemContainerGenerator, _tv.Items, _item);
        }

        private static TreeViewItem ContainerFromItem(ItemContainerGenerator _parentICG, ItemCollection _items, object _item)
        {
            if (_parentICG == null || _items == null || _items.Count < 1 || _item == null)
                return null;

            foreach (object curChildItem in _items)
            {
                TreeViewItem parentContainer = (TreeViewItem)_parentICG.ContainerFromItem(curChildItem);
                if (parentContainer == null)
                    continue;

                TreeViewItem containerThatMightContainItem = (TreeViewItem)parentContainer.ItemContainerGenerator.ContainerFromItem(_item);
                if (containerThatMightContainItem != null)
                    return containerThatMightContainItem;

                TreeViewItem recursionResult = ContainerFromItem(parentContainer.ItemContainerGenerator, parentContainer.Items, _item);
                if (recursionResult != null)
                    return recursionResult;
            }

            return null;

        }
    }
}
