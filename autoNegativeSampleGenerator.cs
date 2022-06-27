using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class autoNegativeSampleGenerator : MonoBehaviour
{
    public GameObject plant;
    Vector3 rotate_vector;
    Vector3 trans;

    //토마토와 mask를 rendering할 camera
    public Camera tomato_camera;
    public Camera mask_camera;
    //camera의 light component
    //조명을 다양화해서 학습데이터 만들고자 할 때 필요
    public Light light;
    //단색배경이 아닌 배경을 하고자 할 때 필요 
    //해당 object에 changeMaterial 스크립트 추가 필요
    public GameObject background;
    float x_offset;
    float y_offset;
    float z_offset;
    //image 해상도
    private int resWidth;
    private int resHeight;

    //저장 경로 root
    string path;

    int dir_count = 0;
    int file_count = 0;
    string currentDir;

    //annotation 저장 directory
    string masks_dir;
    string camk_dir;
    string labels_dir;
    string models_dir;
    string rgb_dir;
    string rotations_dir;
    string stem_rotations_dir;
    string translations_dir;
    string translationsxy2D_dir;

    //annotation file 이름
    string masks_file;
    string camk_file;
    string labels_file;
    string models_file;
    string rotations_file;
    string stem_rotations_file;
    string translations_file;
    string translationsxy2D_file;

    //3D bounding box 코너 -> image projection
    //EfficientPose에서는 크게 필요x
    Vector3 projected;
    Vector3[] points = new Vector3[9];

    //file writer
    StreamWriter camk_writer;
    StreamWriter labels_writer;
    StreamWriter rotations_writer;
    StreamWriter stem_rotations_writer;
    StreamWriter translations_writer;
    StreamWriter translationsxy2D_writer;

    // Start is called before the first frame update
    void Start()
    {
        /***********경로 생성***********/
        resWidth = Screen.width;
        resHeight = Screen.height;
        currentDir = System.IO.Directory.GetCurrentDirectory();
        path = currentDir + "\\ScreenShot\\";

        try
        {
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
            else
            {
                UnityEngine.Debug.Log("존재");

            }
        }
        catch
        {
            UnityEngine.Debug.Log("디렉토리 생성error");
        }
        System.IO.DirectoryInfo curdir = new System.IO.DirectoryInfo(path);
        dir_count = 0;
        foreach (var item in curdir.GetDirectories())
        {
            UnityEngine.Debug.Log(item.FullName);
            dir_count++;
        }
        currentDir = string.Format("{0}{1}", path, dir_count);

        masks_dir = currentDir + "\\masks\\";
        camk_dir = currentDir + "\\cam_k\\";
        labels_dir = currentDir + "\\labels\\";
        models_dir = currentDir + "\\models\\";
        rgb_dir = currentDir + "\\rgb\\";
        rotations_dir = currentDir + "\\rotations\\";
        stem_rotations_dir = currentDir + "\\stem_rotations\\";
        translations_dir = currentDir + "\\translations\\";
        translationsxy2D_dir = currentDir + "\\translations_x_y_2D\\";

        try
        {
            System.IO.Directory.CreateDirectory(currentDir);
            System.IO.Directory.CreateDirectory(masks_dir);
            System.IO.Directory.CreateDirectory(camk_dir);
            System.IO.Directory.CreateDirectory(labels_dir);
            System.IO.Directory.CreateDirectory(models_dir);
            System.IO.Directory.CreateDirectory(rgb_dir);
            System.IO.Directory.CreateDirectory(rotations_dir);
            System.IO.Directory.CreateDirectory(stem_rotations_dir);
            System.IO.Directory.CreateDirectory(translations_dir);
            System.IO.Directory.CreateDirectory(translationsxy2D_dir);
        }
        catch
        {
            UnityEngine.Debug.Log("디렉토리생성불가");
        }

        models_file = models_dir + "models_info.yml";
        StreamWriter models_writer = new StreamWriter(models_file, true);
        models_writer.WriteLine("0: {diameter: 82, min_x: -41.150, min_y: -41.197, min_z: -38.002, size_x: 82.205, size_y: 82.658, size_z: 82.5097}");
        models_writer.WriteLine("1: {diameter: 82, min_x: -41.150, min_y: -41.197, min_z: -38.002, size_x: 82.205, size_y: 82.658, size_z: 82.5097}");
        models_writer.WriteLine("2: {diameter: 82, min_x: -41.150, min_y: -41.197, min_z: -38.002, size_x: 82.205, size_y: 82.658, size_z: 82.5097}");
        models_writer.WriteLine("stem: {diameter: 40, min_x: -5., min_y: -5., min_z: 0., size_x: 10., size_y: 10., size_z: 40.}");

        models_writer.Close();
    }

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(waiter());
    }
    IEnumerator waiter()
    {
        Stopwatch sw = new Stopwatch();

        sw.Start();
        x_offset = Random.Range(-250f, 250f);
        y_offset = Random.Range(-1100f, 1100f);
        z_offset = Random.Range(2500f, 4000f);
        background.transform.position = new Vector3(x_offset, y_offset, z_offset);

        trans = new Vector3(Random.Range(-60f, 60f), Random.Range(-100f, -60f), Random.Range(150f, 250f));
        plant.transform.position = trans;
        rotate_vector = new Vector3(Random.Range(250f, 270f), Random.Range(0f, 90f), Random.Range(0f, 90f));
        plant.transform.eulerAngles = rotate_vector;

        yield return new WaitForSeconds(0.5f);
        UnityEngine.Debug.Log(file_count);
        camk_file = string.Format("{0}{1:D4}.txt", camk_dir, file_count);
        labels_file = string.Format("{0}{1:D4}.txt", labels_dir, file_count);
        rotations_file = string.Format("{0}{1:D4}.txt", rotations_dir, file_count);
        stem_rotations_file = string.Format("{0}{1:D4}.txt", stem_rotations_dir, file_count);
        translations_file = string.Format("{0}{1:D4}.txt", translations_dir, file_count);
        translationsxy2D_file = string.Format("{0}{1:D4}.txt", translationsxy2D_dir, file_count);

        camk_writer = new StreamWriter(camk_file, true);
        labels_writer = new StreamWriter(labels_file, true);
        rotations_writer = new StreamWriter(rotations_file, true);
        stem_rotations_writer = new StreamWriter(stem_rotations_file, true);
        translations_writer = new StreamWriter(translations_file, true);
        translationsxy2D_writer = new StreamWriter(translationsxy2D_file, true);

        //background 설정
        if (background == null)
            tomato_camera.backgroundColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));

        //조명 설정
        //light.intensity = Random.Range(3f, 15f)

        labels_writer.WriteLine(string.Format("-1"));

        rotations_writer.WriteLine(string.Format("0 0 0 0 0 0 0 0 0 0 0"));

        stem_rotations_writer.WriteLine(string.Format("0 0 0 0 0 0 0 0 0"));

        translations_writer.WriteLine(string.Format("0 0 0"));

        translationsxy2D_writer.WriteLine(string.Format("0 0"));


        camk_writer.WriteLine("415 0. 320\n0. 415.57043 240\n0. 0. 1.");

        camk_writer.Close();
        labels_writer.Close();
        rotations_writer.Close();
        stem_rotations_writer.Close();
        translations_writer.Close();
        translationsxy2D_writer.Close();

        Screenshot(rgb_dir, tomato_camera, masks_dir, mask_camera);

        file_count++;

        sw.Stop();

        //UnityEngine.Debug.Log(sw.ElapsedMilliseconds.ToString() + "ms");
    }

    public void Screenshot(string tomato_path, Camera tomato_camera, string mask_path, Camera mask_camera)
    {
        string tomato_name = string.Format("{0}/{1:D4}.png", tomato_path, file_count);

        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        tomato_camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        Rect rec = new Rect(0, 0, screenShot.width, screenShot.height);
        tomato_camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        screenShot.Apply();

        byte[] bytes;
        bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(tomato_name, bytes);

        string mask_name = string.Format("{0}/{1:D4}.png", mask_path, file_count);

        mask_camera.targetTexture = rt;
        Texture2D screenShot_mask = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        Rect rec_mask = new Rect(0, 0, screenShot.width, screenShot.height);
        mask_camera.Render();
        RenderTexture.active = rt;
        screenShot_mask.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        screenShot_mask.Apply();

        bytes = screenShot_mask.EncodeToPNG();
        File.WriteAllBytes(mask_name, bytes);
    }
}