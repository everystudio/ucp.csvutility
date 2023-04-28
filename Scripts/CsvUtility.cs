using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace anogame
{
    [System.Serializable]
    public class CsvModelParam
    {

        public void Load(Dictionary<string, string> param)
        {
            foreach (string key in param.Keys)
            {
                SetField(key.Replace("\"", ""), param[key]);
            }
        }

        public void SetField(string key, string value)
        {
            if (value == null)
            {
                value = "";
            }
            FieldInfo fieldInfo = this.GetType().GetField(key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                return;
            }
            if (fieldInfo.FieldType == typeof(int)) fieldInfo.SetValue(this, int.Parse(value));
            else if (fieldInfo.FieldType == typeof(long)) fieldInfo.SetValue(this, long.Parse(value));
            else if (fieldInfo.FieldType == typeof(string))
            {
                string strValue = value;
                if (value.Contains(':') && value.Contains('{') && value.Contains('}'))
                {
                    // jsonデータ対応用
                }
                else
                {
                    strValue = value.Replace("\"", "");
                }
                fieldInfo.SetValue(this, strValue);
            }
            else if (fieldInfo.FieldType == typeof(float)) fieldInfo.SetValue(this, float.Parse(value));
            else if (fieldInfo.FieldType == typeof(double)) fieldInfo.SetValue(this, double.Parse(value));
            else if (fieldInfo.FieldType == typeof(bool)) fieldInfo.SetValue(this, bool.Parse(value));
            // 他の型にも対応させたいときには適当にここに。enumとかもどうにかなりそう。
        }
        public string GetString(string strKey)
        {
            FieldInfo fieldInfo = this.GetType().GetField(strKey, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                if (fieldInfo.FieldType == typeof(string))
                {
                    return (string)fieldInfo.GetValue(this);
                    //return propertyInfo.GetValue(this, null).ToString();//.Replace("\n", "\r\n");
                }
                else
                {
                    return fieldInfo.GetValue(this).ToString();
                }
            }
            return "";
        }

        public void Set(Dictionary<string, string> dict)
        {
            foreach (string key in dict.Keys)
            {
                FieldInfo fieldInfo = GetType().GetField(key);
                if (fieldInfo.FieldType == typeof(int))
                {
                    int iValue = int.Parse(dict[key]);
                    fieldInfo.SetValue(this, iValue);
                }
                else if (fieldInfo.FieldType == typeof(string))
                {
                    fieldInfo.SetValue(this, dict[key].Replace("\"", ""));
                }
                else if (fieldInfo.FieldType == typeof(double))
                {
                    fieldInfo.SetValue(this, double.Parse(dict[key]));
                }
                else if (fieldInfo.FieldType == typeof(float))
                {
                    fieldInfo.SetValue(this, float.Parse(dict[key]));
                }
                else if (fieldInfo.FieldType == typeof(bool))
                {
                    fieldInfo.SetValue(this, bool.Parse(dict[key]));
                }
                else
                {
                    Debug.LogError("error type unknown");
                }
            }
        }

        public bool Equals(string strWhere)
        {
            string[] div_array = strWhere.Trim().Split(' ');
            bool bRet = true;
            for (int i = 0; i < div_array.Length; i += 4)
            {
                FieldInfo fieldInfo = GetType().GetField(div_array[i]);
                if (fieldInfo.FieldType == typeof(int))
                {
                    int intparam = (int)fieldInfo.GetValue(this);
                    string strJudge = div_array[i + 1];
                    int intcheck = int.Parse(div_array[i + 2]);
                    if (strJudge.Equals("="))
                    {
                        if (intparam != intcheck)
                        {
                            bRet = false;
                        }
                    }
                    else if (strJudge.Equals("!="))
                    {
                        if (intparam == intcheck)
                        {
                            bRet = false;
                        }
                    }
                    else
                    {
                    }
                }
            }
            return bRet;
        }
    }

    [System.Serializable]
    public class CsvModel<T> where T : CsvModelParam, new()
    {
        public List<T> List => list;
        public List<T> All { get { return list; } }
        private List<T> list = new List<T>();
        public bool IsLoaded => loadedActionFinished;

        public CsvModel() { }
        public CsvModel(TextAsset textAsset)
        {
            Load(textAsset);
        }
        public CsvModel(string file)
        {
            Load(file);
        }

        private bool loadedActionFinished;
        private void LoadedAction()
        {
            if (loadedActionFinished == false)
            {
                loadedAction();
                loadedActionFinished = true;
            }
        }
        virtual protected void loadedAction() { }

        virtual public bool Load(string strFilename, string strPath)
        {
            loadedActionFinished = false;

            bool bRet = false;
            list = new List<T>();

            string file = string.Format("{0}.csv", strFilename);
            string fullpath = System.IO.Path.Combine(strPath, file);

            if (System.IO.File.Exists(fullpath) == false)
            {
                Debug.LogError("file not exists:" + fullpath);
                return false;
            }

#if !UNITY_WEBPLAYER
            FileInfo fi = new FileInfo(fullpath);
            StreamReader sr = new StreamReader(fi.OpenRead());

            try
            {
                string strFirst = sr.ReadLine();
                var headerElements = strFirst.Split(',');

                while (sr.Peek() != -1)
                {
                    string strLine = sr.ReadLine();
                    ParseLine(strLine, headerElements);
                }
                sr.Close();
                bRet = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
                bRet = false;
            }
#endif

            if (bRet)
            {
                LoadedAction();
            }
            return bRet;
        }
        virtual public bool Load()
        {
            return Load(saveFilename);
        }
        virtual public bool Load(string _strFilename)
        {
            saveFilename = _strFilename;
            return Load(_strFilename, Application.persistentDataPath);
        }

        virtual public bool Load(TextAsset inputTextAsset)
        {
            bool bRet = false;
            string text = inputTextAsset.text;
            try
            {
                text = text.Trim().Replace("\r", "") + "\n";
                var lines = text.Split('\n').ToList();

                // header
                var headerElements = lines[0].Split(',');
                lines.RemoveAt(0); // header

                // body
                list = new List<T>();
                foreach (var line in lines)
                {
                    ParseLine(line, headerElements);
                }
                LoadedAction();
                bRet = true;
            }
            catch (System.Exception)
            {
                throw;
            }
            return bRet;
        }

        virtual public bool LoadResources(string fileName)
        {
            bool bRet;
            string path = fileName;
            try
            {
                TextAsset textAsset = ((TextAsset)Resources.Load(path, typeof(TextAsset)));
                bRet = Load(textAsset);
            }
            catch (System.Exception ex)
            {
                if (ex != null)
                {
                    Debug.LogError(fileName);
                    Debug.LogError(ex);
                }
                bRet = false;
            }
            return bRet;
        }

        private void ParseLine(string line, string[] headerElements)
        {
            List<string> replace_items = new List<string>();
            int iSubStartIndex = 0;
            int iBucketNum = 0;
            bool bCollect = false;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i].Equals('{'))
                {
                    iBucketNum += 1;
                    if (bCollect == false)
                    {
                        iSubStartIndex = i;
                        bCollect = true;
                    }
                }
                if (line[i].Equals('}'))
                {
                    iBucketNum -= 1;
                    if (bCollect == true)
                    {
                        if (iBucketNum == 0)
                        {
                            bCollect = false;
                            string sub = line.Substring(iSubStartIndex, i - iSubStartIndex);
                            line = line.Remove(iSubStartIndex, i - iSubStartIndex);
                            string strReplaceKey = string.Format("replace_{0}", replace_items.Count);
                            line = line.Insert(iSubStartIndex, strReplaceKey);
                            replace_items.Add(sub);
                            i = iSubStartIndex;
                        }
                    }
                }
            }

            var elements = line.Split(',');

            if (elements.Length == 1) return;
            if (elements.Length != headerElements.Length)
            {
                Debug.LogWarning(string.Format("can't load: {0}", line));
                return;
            }

            if (0 < replace_items.Count)
            {
                for (int i = 0; i < replace_items.Count; i++)
                {
                    string strReplaceKey = string.Format("replace_{0}", i);

                    for (int j = 0; j < elements.Length; j++)
                    {
                        if (elements[j].Contains(strReplaceKey))
                        {
                            elements[j] = elements[j].Replace(strReplaceKey, replace_items[i]);
                        }
                    }
                }
            }

            var param = new Dictionary<string, string>();
            for (int i = 0; i < elements.Length; ++i)
            {
                param.Add(headerElements[i], elements[i]);
            }
            var master = new T();
            master.Load(param);
            list.Add(master);
        }

        private string saveFilename;
        public void SetSaveFilename(string strFilename)
        {
            saveFilename = strFilename;
        }
        public void Save()
        {
            try
            {
                if (saveFilename.Length == 0)
                {
                    throw new System.Exception("no set saveFilename");
                }
                Save(saveFilename);
            }
            catch
            {
                ;// 特に何をするわけでもない
                Debug.LogError("no set savefilename");
            }
        }
        public void Save(string strFilename)
        {
            // 保存前に処理をしたい場合実装する
            preSave();
            if (strFilename.Equals("") == true)
            {
                strFilename = typeof(T).ToString();
            }
            save(strFilename);
        }

        // 同じディレクトリにある名前違いのファイルを移動させる
        private void fileMove(string _strPath, string _strSource, string _strDest)
        {
            System.IO.File.Copy(
                System.IO.Path.Combine(_strPath, _strSource),
                System.IO.Path.Combine(_strPath, _strDest),
                true);

            System.IO.File.Delete(System.IO.Path.Combine(_strPath, _strSource));
        }

        private bool WritableField(FieldInfo info)
        {
            bool bRet = false;

            if (info.FieldType == typeof(int))
            {
                bRet = true;
            }
            else if (info.FieldType == typeof(float))
            {
                bRet = true;
            }
            else if (info.FieldType == typeof(string))
            {
                bRet = true;
            }
            else if (info.FieldType == typeof(bool))
            {
                bRet = true;
            }
            else
            {
                //Debug.LogError(_info.PropertyType);
            }
            return bRet;
        }

        /*
		 * 特に指定が無い場合は自動書き込み
		 * 独自実装をしたい場合は個別にoverrideしてください
		 * */
        virtual protected void save(string _strFilename)
        {
            //Debug.LogWarning (string.Format( "kvs.save {0}" , list.Count));
            //int test = 0;
            //Debug.Log(test++);
            StreamWriter sw;
            try
            {
                string strTempFilename = string.Format("{0}.csv.tmp", _strFilename);
                Directory.CreateDirectory(strTempFilename);
                FileInfo fi = new FileInfo(System.IO.Path.Combine(Application.persistentDataPath, strTempFilename));
                sw = fi.AppendText();

                T dummy = new T();
                FieldInfo[] infoArray = dummy.GetType().GetFields();
                bool bIsFirst = true;
                string strHead = "";
                foreach (FieldInfo info in infoArray)
                {
                    if (!WritableField(info))
                    {
                        continue;
                    }
                    if (bIsFirst == true)
                    {
                        bIsFirst = false;
                    }
                    else
                    {
                        strHead += ",";
                    }
                    strHead += info.Name;
                }

                sw.WriteLine(strHead);
                foreach (T data in list)
                {
                    bIsFirst = true;
                    string strData = "";
                    foreach (FieldInfo info in infoArray)
                    {
                        if (!WritableField(info))
                        {
                            continue;
                        }

                        if (bIsFirst == true)
                        {
                            bIsFirst = false;
                        }
                        else
                        {
                            strData += ",";
                        }
                        strData += data.GetString(info.Name);
                    }
                    sw.WriteLine(strData);

                }
                sw.Flush();
                sw.Close();

                fileMove(
                    Application.persistentDataPath,
                    string.Format("{0}.csv.tmp", _strFilename),
                    string.Format("{0}.csv", _strFilename));
            }
            catch (System.Exception ex)
            {
                Debug.LogError(_strFilename);
                Debug.LogError(ex);
                return;
            }
            return;
        }

        protected virtual void preSave()
        {
            return;
        }

        public virtual List<T> Select(string _strWhere)
        {
            List<T> ret_list = new List<T>();
            foreach (T param in list)
            {
                if (param.Equals(_strWhere))
                {
                    ret_list.Add(param);
                }
            }
            return ret_list;
        }

        public virtual T SelectOne(string _strWhere)
        {
            List<T> ret_list = new List<T>();
            foreach (T param in list)
            {
                if (param.Equals(_strWhere))
                {
                    ret_list.Add(param);
                }
            }
            if (0 < ret_list.Count)
            {
                return ret_list[0];
            }
            return new T();
        }

        virtual public int Update(Dictionary<string, string> _dictUpdate, string _strWhere)
        {
            List<T> update_list = new List<T>();
            update_list = Select(_strWhere);
            foreach (T param in update_list)
            {
                param.Set(_dictUpdate);
            }
            return update_list.Count;
        }

        public void CheckDebugLog()
        {
            string dataLogger = "";
            foreach (var param in All)
            {
                dataLogger += param.ToString() + "\n";
            }
            Debug.Log(dataLogger);
        }
    }
}