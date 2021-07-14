// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Data
{
    /// <summary>
    /// Simple data data source that generates a large list of randomly generated entries. 
    /// This can be used to test paging and virtualization.
    /// </summary>
    /// 
    /// <remarks>
    /// </remarks>

    public class DataSourceDynamicList : DataSourceGOBase
    {

        [SerializeField]
        protected int _collectionSize = 200;

        protected int _counter = 0;

        public override IDataSource AllocateDataSource()
        { 
            if (_dataSource == null)
            {
                _dataSource = new DataSourceObjects();
            }

            return _dataSource;
        }


        // Update is called once per frame
        void Update()
        {
            UpdateAllData();

            _counter++;

        }

        protected void UpdateAllData()
        {
            DataChangeSetBegin();


            DataChangeSetEnd();
        }


        protected override void InitializeDataSource()
        {
            // Enable either the dynamic or static versions of 
            // the same data set structure.

            InitializeDataSourceDynamic();
            //InitializeDataSourceStatic();
        }


        protected void InitializeDataSourceDynamic()
        {
            int maxImageId = 101;
            string[] dates = { "February 26, 2020", "January 1, 2000", "March 1, 2018", "April 29, 2017", "May 5, 1999", "June 21, 1980" };
            string[] words = { "mixed reality", "MRTK", "HoloLens", "Unity", "Visual Studio", "Middleware", "Microsoft", "XR", "VR", "AR", "MR", "C#", "Azure" };

            System.Random r = new System.Random();

            DataChangeSetBegin();

            for (int i = 0; i < _collectionSize; i++)
            {
                int imageId = 1 + (i % maxImageId);
                string pathBase = String.Format("images[{0:d}]", i);
                string id = String.Format("{0:D6}", i);
                string title = String.Format("Image #{0:d}", i);
                string url = String.Format("http://michaelinfo.com/nature-photos/images/{0:D4}.jpg", imageId );

                Randomize(words);
                string description = "";
                for (int w = 0; w < 5; w++)
                {
                    description = description + words[w] + " ";
                }

                SetValue(pathBase + ".id", id);
                SetValue(pathBase + ".title", title);
                SetValue(pathBase + ".description", description);
                SetValue(pathBase + ".imageUrl", url);

            }

            DataChangeSetEnd();


            NotifyAllChanged();

        }

        protected void Randomize(string[] items)
        {
            System.Random rand = new System.Random();

            // For each spot in the array, pick
            // a random item to swap into that spot.
            for (int i = 0; i < items.Length - 1; i++)
            {
                int j = rand.Next(i, items.Length);
                string temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }

        /// <summary>
        /// ALternative static list of entries of the same data structure as the
        /// randomly generated entries of InitializeDataSourceDynamic().
        /// </summary>
        /// 
        protected void InitializeDataSourceStatic()
        {
            /****
             *  Equivalent JSON to the programmatically generated data set.
             * 
             * { "images" : [
        { "id" : "0001", "imageUrl" : "http://michaelinfo.com/test/images/0001.jpeg",
          "title" : "Stone Mountain and lone tree",
            "description" : "Taken while rock climbing on Stone Mountain in NC."
        },
        { "id" : "0002", "imageUrl" : "http://michaelinfo.com/test/images/0002.jpeg",
          "title" : "Brain Celosia flower",
            "description" : "Found this amazing flower in NC."
        },
        { "id" : "0003", "imageUrl" : "http://michaelinfo.com/test/images/0003.jpeg",
          "title" : "Snow blown tree trunk",
            "description" : "Hiking in Mt Hood, Oregon."
        },
        { "id" : "0008", "imageUrl" : "http://michaelinfo.com/test/images/0008.jpeg",
          "title" : "Colorful tree trunk",
            "description" : "Humans + nature becomes art in this cut tree trunk."
        },
        { "id" : "0010", "imageUrl" : "http://michaelinfo.com/test/images/0010.jpeg",
          "title" : "Geothermal steam through trees",
            "description" : "Hiking in Costa Rica, ran into this ephemeral display of geothermal steam and sunlight."
        },
        { "id" : "0011", "imageUrl" : "http://michaelinfo.com/test/images/0011.jpeg",
          "title" : "Crazy tree trunk",
            "description" : "Encountered on hike at Rincón de la Vieja Volcano in Costa Rica."
        }

    ]}
            ****/
            DataChangeSetBegin();

            SetValue("images[0].id", "0001");
            SetValue("images[0].title", "Stone Mountain and lone tree");
            SetValue("images[0].description", "Taken while rock climbing on Stone Mountain in NC.");
            SetValue("images[0].imageUrl", "http://michaelinfo.com/test/images/0001.jpeg");

            SetValue("images[1].id", "0002");
            SetValue("images[1].title", "Brain Celosia flower");
            SetValue("images[1].description", "Found this amazing flower in NC.");
            SetValue("images[1].imageUrl", "http://michaelinfo.com/test/images/0002.jpeg");

            DataChangeSetEnd();

        }

    }

}
