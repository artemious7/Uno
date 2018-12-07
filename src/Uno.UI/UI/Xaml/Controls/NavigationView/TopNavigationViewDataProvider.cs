using System;

namespace Windows.UI.Xaml.Controls
{
	internal class TopNavigationViewDataProvider
	{
		public TopNavigationViewDataProvider(NavigationView owner)
		{
			Func<object, object> lambda = (object value) =>
			{
				return IndexOf(value);
			};

			var primaryVector = new SplitVector(m_owner, PrimaryList, lambda);
			var overflowVector = new SplitVector(m_owner, OverflowList, lambda);

			InitializeSplitVectors(primaryVector, overflowVector);
		}

		IVector<object> GetPrimaryItems()
		{
			return GetVector(PrimaryList)->GetVector();
		}

		IVector<object> GetOverflowItems()
		{
			return GetVector(OverflowList)->GetVector();
		}

		// The raw data is from MenuItems or MenuItemsSource
		void SetDataSource(object rawData)
		{
			if (ShouldChangeDataSource(rawData)) // avoid to create multiple of datasource for the same raw data
			{
				ItemsSourceView dataSource = nullptr;
				if (rawData)
				{
					dataSource = ItemsSourceView(rawData);
				}
				ChangeDataSource(dataSource);
				m_rawDataSource.set(rawData);
				if (dataSource)
				{
					MoveAllItemsToPrimaryList();
				}
			}
		}

		bool ShouldChangeDataSource(object rawData)
		{
			return rawData != m_rawDataSource;    
		}

		void OnRawDataChanged(Action<NotifyCollectionChangedEventArgs> dataChangeCallback)
		{
			m_dataChangeCallback = dataChangeCallback;
		}

		int IndexOf(object  value)
		{
			var dataSource = m_dataSource;
			if (dataSource != null)
			{
				auto inspectingDataSource = static_cast<InspectingDataSource*>(get_self<ItemsSourceView>(dataSource));

				return inspectingDataSource->IndexOf(value);
			}
			return -1;
		}

		object GetAt(int index)
		{
			if (auto dataSource = m_dataSource)
			{
				return dataSource.GetAt(index);
			}
			return nullptr;
		}

		int Size()
		{
			if (auto dataSource = m_dataSource)
			{
				return static_cast<int>(dataSource.Count());
			}
			return 0;
		}

		NavigationViewSplitVectorID DefaultVectorIDOnInsert()
		{
			return NotInitialized;
		}

		float DefaultAttachedData()
		{
			return float.MinValue;
		}

		void MoveAllItemsToPrimaryList()
		{
			for (int i = 0; i < Size(); i++)
			{
				MoveItemToVector(i, PrimaryList);
			}
		}

		List<int> ConvertPrimaryIndexToIndex(List<int> indexesInPrimary)
		{
			List<int> indexes;
			if (!indexesInPrimary.empty())
			{
				auto vector = GetVector(PrimaryList);
				if (vector)
				{
					// transform PrimaryList index to OrignalVector index
					std::transform(indexesInPrimary.begin(), indexesInPrimary.end(), std::back_inserter(indexes),

						[&vector](int index)-> int

					{
						return vector->IndexToIndexInOriginalVector(index);
					});
				}
			}
			return indexes;
		}

		void MoveItemsOutOfPrimaryList(List<int> indexes)
		{
			MoveItemsToList(indexes, OverflowList);
		}

		void MoveItemsToPrimaryList(List<int> indexes)
		{
			MoveItemsToList(indexes, PrimaryList);
		}

		void MoveItemsToList(List<int> indexes, NavigationViewSplitVectorID vectorID)
		{
			for (auto & index : indexes)
			{
				MoveItemToVector(index, vectorID);
			};
		}

		int GetPrimaryListSize()
		{
			return GetPrimaryItems().Size();
		}

		int GetNavigationViewItemCountInPrimaryList()
		{
			int count = 0;
			for (int i = 0; i < Size(); i++)
			{
				if (IsItemInPrimaryList(i) && IsContainerNavigationViewItem(i))
				{
					count++;
				}
			}
			return count;
		}

		int GetNavigationViewItemCountInTopNav()
		{
			int count = 0;
			for (int i = 0; i < Size(); i++)
			{
				if (IsContainerNavigationViewItem(i))
				{
					count++;
				}
			}
			return count;
		}

		void UpdateWidthForPrimaryItem(int indexInPrimary, float width)
		{
			auto vector = GetVector(PrimaryList);
			if (vector)
			{
				auto index = vector->IndexToIndexInOriginalVector(indexInPrimary);
				SetWidthForItem(index, width);
			}
		}

		float WidthRequiredToRecoveryAllItemsToPrimary()
		{
			auto width = 0.f;
			for (int i = 0; i < Size(); i++)
			{
				if (!IsItemInPrimaryList(i))
				{
					width += GetWidthForItem(i);
				}
			}
			width -= m_overflowButtonCachedWidth;
			return std::max(0.f, width);
		}

		bool HasInvalidWidth(List<int> & items)
		{
			bool hasInvalidWidth = false;
			for (auto & index : items)
			{
				if (!IsValidWidthForItem(index))
				{
					hasInvalidWidth = true;
					break;
				}
			}
			return hasInvalidWidth;
		}

		float GetWidthForItem(int index)
		{
			auto width = AttachedData(index);
			if (!IsValidWidth(width))
			{
				width = 0;
			}
			return width;
		}

		float CalculateWidthForItems(List<int> &items)
		{
			float width = 0.f;
			for (auto & index : items)
			{
				width += GetWidthForItem(index);
			}
			return width;
		}

		void InvalidWidthCache()
		{
			ResetAttachedData(-1.0f);
		}

		float OverflowButtonWidth()
		{
			return m_overflowButtonCachedWidth;
		}

		void OverflowButtonWidth(float width)
		{
			m_overflowButtonCachedWidth = width;
		}

		bool IsItemSelectableInPrimaryList(const object  value)
		{
			int index = IndexOf(value);
			return (index != -1);
		}

		int IndexOf(const object  value, NavigationViewSplitVectorID vectorID)
		{
			return IndexOfImpl(value, vectorID);
		}

		void OnDataSourceChanged(const object  sender, const NotifyCollectionChangedEventArgs& args)
		{
			switch (args.Action())
			{
				case NotifyCollectionChangedAction::Add:
					{
						OnInsertAt(args.NewStartingIndex(), args.NewItems().Size());
						break;
					}

				case NotifyCollectionChangedAction::Remove:
					{
						OnRemoveAt(args.OldStartingIndex(), args.OldItems().Size());
						break;
					}

				case NotifyCollectionChangedAction::Reset:
					{
						OnClear();
						break;
					}

				case NotifyCollectionChangedAction::Replace:
					{
						OnRemoveAt(args.OldStartingIndex(), args.OldItems().Size());
						OnInsertAt(args.NewStartingIndex(), args.NewItems().Size());
						break;
					}
			}
			if (m_dataChangeCallback)
			{
				m_dataChangeCallback(args);
			}
		}

		bool IsValidWidth(float width)
		{
			return (width >= 0) && (width < std::numeric_limits<float>::max());
		}

		bool IsValidWidthForItem(int index)
		{
			auto width = AttachedData(index);
			return IsValidWidth(width);
		}

		void InvalidWidthCacheIfOverflowItemContentChanged()
		{
			bool shouldRefreshCache = false;
			for (int i = 0; i < Size(); i++)
			{
				if (!IsItemInPrimaryList(i))
				{
					if (auto navItem = GetAt(i).try_as<NavigationViewItem>())
					{
				auto itemPointer = get_self<NavigationViewItem>(navItem);
				if (itemPointer->IsContentChangeHandlingDelayedForTopNav())
				{
					itemPointer->ClearIsContentChangeHandlingDelayedForTopNavFlag();
					shouldRefreshCache = true;
				}
			}
		}
			}

			if (shouldRefreshCache)
			{
				InvalidWidthCache();
			}
		}

		void SetWidthForItem(int index, float width)
		{
			if (IsValidWidth(width))
			{
				AttachedData(index, width);
			}
		}

		void ChangeDataSource(ItemsSourceView newValue)
		{
			auto oldValue = m_dataSource;
			if (oldValue != newValue)
			{
				// update to the new datasource.

				if (oldValue)
				{
					oldValue.CollectionChanged(m_dataSourceChanged);
				}

				Clear();

				m_dataSource.set(newValue);
				SyncAndInitVectorFlagsWithID(NotInitialized, DefaultAttachedData());

				if (newValue)
				{
					m_dataSourceChanged = newValue.CollectionChanged({ this, &OnDataSourceChanged });
				}
			}

			// Move all to primary list
			MoveItemsToVector(NotInitialized);
		}

		bool IsItemInPrimaryList(int index)
		{
			return GetVectorIDForItem(index) == PrimaryList;
		}

		bool IsContainerNavigationViewItem(int index)
		{
			bool isContainerNavigationViewItem = true;

			auto item = GetAt(index);
			if (item && (item.try_as<NavigationViewItemHeader>() || item.try_as<NavigationViewItemSeparator>()))
			{
				isContainerNavigationViewItem = false;
			}
			return isContainerNavigationViewItem;
		}

		bool IsContainerNavigationViewHeader(int index)
		{
			bool isContainerNavigationViewHeader = false;

			auto item = GetAt(index);
			if (item && item.try_as<NavigationViewItemHeader>())
			{
				isContainerNavigationViewHeader = true;
			}
			return isContainerNavigationViewHeader;
		}

	}
}
