using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace ComponentBuilder.WpfUtils
{
    class ImageRecordManager
    {
        private long nr_images;
        private List<ImageRecord> images_on_record;

        public IReadOnlyList<ImageRecord> ImagesForDisplay
        { 
            get 
            {
                List<ImageRecord> list = new List<ImageRecord>{ ImageRecord.EMPTY };
                list.AddRange(this.images_on_record);
                return list.AsReadOnly(); 
            } 
        }

        public ImageRecordManager()
        {
            this.nr_images = 0;
            this.images_on_record = new List<ImageRecord>();
        }

        public void AddRecord(string _file)
        {
            ImageRecord ir = new ImageRecord((++this.nr_images), _file);
            this.images_on_record.Add(ir);
        }

        public void RemoveRecord(ImageRecord _ir)
        {
            if (_ir == null) return;
            this.images_on_record.Remove(_ir);
            if (_ir.ID == this.nr_images)
            {
                // recalculate
                this.nr_images = 0;
                foreach(ImageRecord i in this.images_on_record)
                {
                    this.nr_images = Math.Max(this.nr_images, i.ID);
                }
            }
        }

        public void SaveRecordToFile(string _filename)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream stream = File.Create(_filename))
            {
                bf.Serialize(stream, this.images_on_record);
            }
        }

        public void LoadRecordsFromFile(string _filename)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream stream = File.Open(_filename, FileMode.Open))
            {
                this.images_on_record = (List<ImageRecord>)bf.Deserialize(stream);
            }

            this.nr_images = 0;
            foreach (ImageRecord ir in this.images_on_record)
            {
                this.nr_images = Math.Max(this.nr_images, ir.ID);
            }
        }
    }
}
