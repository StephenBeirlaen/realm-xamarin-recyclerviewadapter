using System;
using System.Linq;

using Android.Support.V7.Widget;
using Realms;

namespace RealmShopDemo.Droid.Adapters
{
    // Adapted from Realm Android to work on Xamarin
    // By Stephen Beirlaen
    // https://github.com/realm/realm-android-adapters/blob/master/adapters/src/main/java/io/realm/RealmRecyclerViewAdapter.java

    /**
    * The RealmBaseRecyclerAdapter class is an abstract utility class for binding RecyclerView UI elements to Realm data.
    * <p>
    * This adapter will automatically handle any updates to its data and call {@code notifyDataSetChanged()},
    * {@code notifyItemInserted()}, {@code notifyItemRemoved()} or {@code notifyItemRangeChanged(} as appropriate.
    * <p>
    * The RealmAdapter will stop receiving updates if the Realm instance providing the {@link OrderedRealmCollection} is
    * closed.
    *
    * @param <T> type of {@link RealmModel} stored in the adapter.
    */
    public abstract class RealmRecyclerViewAdapter<T> : RecyclerView.Adapter where T : RealmObject
    {
        private bool _hasAutoUpdates;
        private NotificationCallbackDelegate<T> _listener;
        private IRealmCollection<T> _adapterData;
        private IDisposable _notificationSubscriptionToken;

        private void CreateListener(IRealmCollection<T> sender, ChangeSet changeSet, Exception error)
        {
            // null Changes means the async query returns the first time.
            if (changeSet == null)
            {
                NotifyDataSetChanged();
                return;
            }
            // For deletions, the adapter has to be notified in reverse order.
            int[] deletions = changeSet.DeletedIndices;
            for (int i = deletions.Length - 1; i >= 0; i--) // Todo: Improve with OrderedCollectionChangeSet like Android example - not available (yet)
            {
                NotifyItemRemoved(deletions[i]);
            }

            int[] insertions = changeSet.InsertedIndices;
            for (int i = insertions.Length - 1; i >= 0; i--)
            {
                NotifyItemInserted(insertions[i]);
            }

            int[] modifications = changeSet.ModifiedIndices;
            for (int i = modifications.Length - 1; i >= 0; i--)
            {
                NotifyItemChanged(modifications[i]);
            }
        }

        protected RealmRecyclerViewAdapter(IRealmCollection<T> data, bool autoUpdate)
        {
            /*if (data != null && !data.IsManaged()) // Todo: IsManaged function is not available on IRealmCollection (yet)
                throw new InvalidOperationException("Only use this adapter with managed IRealmCollections, for un-managed lists you can just use the BaseRecyclerViewAdapter");*/
            _adapterData = data;
            _hasAutoUpdates = autoUpdate;

            if (_hasAutoUpdates)
            {
                _listener = CreateListener;
            }
            else
            {
                _listener = null;
            }
        }

        public override void OnAttachedToRecyclerView(RecyclerView recyclerView)
        {
            base.OnAttachedToRecyclerView(recyclerView);
            if (_hasAutoUpdates && IsDataValid())
            {
                AddListener(_adapterData);
            }
        }

        public override void OnDetachedFromRecyclerView(RecyclerView recyclerView)
        {
            base.OnDetachedFromRecyclerView(recyclerView);
            if (_hasAutoUpdates && IsDataValid())
            {
                RemoveListener();
            }
        }

        /**
         * Returns the current ID for an item. Note that item IDs are not stable so you cannot rely on the item ID being the
         * same after notifyDataSetChanged() or {@link #updateData(OrderedRealmCollection)} has been called.
         *
         * @param index position of item in the adapter.
         * @return current item ID.
         */

        public override long GetItemId(int position)
        {
            return position;
        }

        public override int ItemCount => IsDataValid() ? _adapterData.Count() : 0;

        /**
         * Returns the item associated with the specified position.
         * Can return {@code null} if provided Realm instance by {@link OrderedRealmCollection} is closed.
         *
         * @param index index of the item.
         * @return the item at the specified position, {@code null} if adapter data is not valid.
         */
        public T GetItem(int index)
        {
            return IsDataValid() ? _adapterData.ElementAt(index) : null;
        }

        /**
         * Returns data associated with this adapter.
         *
         * @return adapter data.
         */
        public IRealmCollection<T> GetData()
        {
            return _adapterData;
        }

        /**
         * Updates the data associated to the Adapter. Useful when the query has been changed.
         * If the query does not change you might consider using the automaticUpdate feature.
         *
         * @param data the new {@link OrderedRealmCollection} to display.
         */
        public void UpdateData(IRealmCollection<T> data)
        {
            if (_hasAutoUpdates)
            {
                if (IsDataValid())
                {
                    RemoveListener();
                }
                if (data != null)
                {
                    AddListener(data);
                }
            }

            _adapterData = data;
            NotifyDataSetChanged();
        }

        private void AddListener(IRealmCollection<T> data)
        {
            // Only one type of collection in Realm Xamarin
            IRealmCollection<T> results = data;
            _notificationSubscriptionToken = results.SubscribeForNotifications(_listener);
        }

        private void RemoveListener()
        {
            if (_notificationSubscriptionToken != null)
            {
                _notificationSubscriptionToken.Dispose();
                _notificationSubscriptionToken = null;
            }
        }

        private bool IsDataValid()
        {
            return _adapterData != null && _adapterData.IsValid;
        }
    }
}
