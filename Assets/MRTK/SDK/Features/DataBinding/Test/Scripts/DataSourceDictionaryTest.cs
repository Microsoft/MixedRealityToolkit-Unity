// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Data
{




    /// <summary>
    /// Simple test data source that programmatically changes variables in a data source. 
    /// </summary>
    /// 
    /// <remarks>
    /// Using a simple <key,value> store, it's possible to separate data from view
    /// to simplify the integration of generic view prefabs that are populated from
    /// external information.
    /// </remarks>

    public class DataSourceDictionaryTest : DataSourceGOBase
    {
        private float _deltaSeconds;
        private int _nextOneSecondTarget;
        private int _nextFiveSecondTarget;
        private bool _styleYellow;
        private int _status = 0;
        private int _score = 100;

        public override IDataSource AllocateDataSource()
        {
            if (_dataSource == null)
            {
                _dataSource = new DataSourceDictionary();
            }

            return _dataSource;
        }

        protected override void InitializeDataSource()
        {
            InitializeData();
            _deltaSeconds = 0;
            _nextOneSecondTarget = 0;
        }

        // Update is called once per frame
        protected void Update()
        {
            string[] statusText = { "open", "pending", "cancelled", "inprogress", "completed" };
            _deltaSeconds += Time.deltaTime;
            int tenthsOfSeconds = (int)(_deltaSeconds * 10.0);

            DataChangeSetBegin();


            if (tenthsOfSeconds > _nextFiveSecondTarget )
            {
                if ( _styleYellow)
                {
                    SetValue("stylesheet", "standard");
                } else
                {
                    SetValue("stylesheet", "yellow");
                }
                _styleYellow = !_styleYellow;
                _nextFiveSecondTarget += 50;
            }

            if ( tenthsOfSeconds > _nextOneSecondTarget)
            {
                _nextOneSecondTarget += 10;
                SetValue("score", ++_score);

                if ((++_status % statusText.Length) == 0)
                {
                    _status = 0;
                }

                SetValue("status", statusText[_status]);
            }

            DataChangeSetEnd();

        }

        private void InitializeData()
        {
            DataChangeSetBegin();
            SetValue("firstname", "Michael");
            SetValue("lastname", "Hoffman");
            SetValue("score", 123);
            SetValue("status", "open");
            SetValue("stylesheet", "standard");
            DataChangeSetEnd();
        }
    }
}
