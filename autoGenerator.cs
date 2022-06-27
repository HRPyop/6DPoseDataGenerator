using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class autoGenerator : MonoBehaviour
{
    public Color color = Color.green;
    
    //Scene 상에서 3D bounding box visuallization 위한 설정
    private Vector3 v3FrontTopLeft;
    private Vector3 v3FrontTopRight;
    private Vector3 v3FrontBottomLeft;
    private Vector3 v3FrontBottomRight;
    private Vector3 v3BackTopLeft;
    private Vector3 v3BackTopRight;
    private Vector3 v3BackBottomLeft;
    private Vector3 v3BackBottomRight;
    Vector3 v3Center;
    LineDrawer[,] lineDrawer;
    public float linesize = 0.2f;

    //tomato와 줄기(stem) list (토마토와 이에 해당하는 줄기가 같은 순서대로 저장 되어야 함)
    public GameObject[] tomato_list;
    public GameObject[] plant_list;
    //main 줄기
    public GameObject plant; 

    //토마토와 mask를 rendering할 camera
    public Camera tomato_camera;
    public Camera mask_camera;
    //camera의 light component
    //조명을 다양화해서 학습데이터 만들고자 할 때 필요
    public Light light;
    //단색배경이 아닌 배경을 하고자 할 때 필요 
    //해당 object에 changeMaterial 스크립트 추가 필요
    public GameObject background;

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

    //plant 회전, 이동
    Vector3 rotate_vector;
    float rotate_angle;
    Vector3 cv_rodrigues;
    Vector3 trans;

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

    Vector3[] spots; //tomato의 random 생성 위치
    Vector3[] froms; //tomato의 위치에 따른 연결되는 줄기 위치
    int[] check; //생성된 tomato 갯수 확인

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
        /**********3D bounding box 생성**********/
        lineDrawer = new LineDrawer[tomato_list.Length,12];
        for (int k = 0; k < tomato_list.Length; k++)
        {
            GameObject tomato = tomato_list[k];
            for (int i = 0; i < 12; i++)
            {
                lineDrawer[k,i] = new LineDrawer(linesize);
            }


            Bounds bounds = tomato.GetComponent<MeshFilter>().mesh.bounds;
            v3Center = bounds.center;
            Vector3 v3Extents = bounds.extents;

            //local points
            v3FrontTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top left corner
            v3FrontTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top right corner
            v3FrontBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom left corner
            v3FrontBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom right corner
            v3BackTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top left corner
            v3BackTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top right corner
            v3BackBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom left corner
            v3BackBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom right corner

            //world points
            v3Center = tomato.transform.TransformPoint(v3Center);
            v3FrontTopLeft = tomato.transform.TransformPoint(v3FrontTopLeft);
            v3FrontTopRight = tomato.transform.TransformPoint(v3FrontTopRight);
            v3FrontBottomLeft = tomato.transform.TransformPoint(v3FrontBottomLeft);
            v3FrontBottomRight = tomato.transform.TransformPoint(v3FrontBottomRight);
            v3BackTopLeft = tomato.transform.TransformPoint(v3BackTopLeft);
            v3BackTopRight = tomato.transform.TransformPoint(v3BackTopRight);
            v3BackBottomLeft = tomato.transform.TransformPoint(v3BackBottomLeft);
            v3BackBottomRight = tomato.transform.TransformPoint(v3BackBottomRight);
            DrawBox(k);
            
            points[0] = v3Center;
            points[1] = v3FrontTopLeft;
            points[2] = v3FrontTopRight;
            points[3] = v3FrontBottomLeft;
            points[4] = v3FrontBottomRight;
            points[5] = v3BackTopLeft;
            points[6] = v3BackTopRight;
            points[7] = v3BackBottomLeft;
            points[8] = v3BackBottomRight;
        }

        models_file = models_dir + "models_info.yml";
        StreamWriter models_writer = new StreamWriter(models_file, true);
        models_writer.WriteLine("0: {diameter: 82, min_x: -41.150, min_y: -41.197, min_z: -38.002, size_x: 82.205, size_y: 82.658, size_z: 82.5097}");
        models_writer.WriteLine("1: {diameter: 82, min_x: -41.150, min_y: -41.197, min_z: -38.002, size_x: 82.205, size_y: 82.658, size_z: 82.5097}");
        models_writer.WriteLine("2: {diameter: 82, min_x: -41.150, min_y: -41.197, min_z: -38.002, size_x: 82.205, size_y: 82.658, size_z: 82.5097}");
        models_writer.WriteLine("stem: {diameter: 40, min_x: -5., min_y: -5., min_z: 0., size_x: 10., size_y: 10., size_z: 40.}");

        models_writer.Close();

        //메인 줄기 기준 tomato가 생성될 곳들의 위치
        spots = new Vector3[7];
        spots[0] = new Vector3(-29, -71, 93);
        spots[1] = new Vector3(90, -30, 36);
        spots[2] = new Vector3(0, 56, 26);
        spots[3] = new Vector3(-33, -74, 1);
        spots[4] = new Vector3(-4, 142, 70);
        spots[5] = new Vector3(6, 60, 109);
        spots[6] = new Vector3(-61, 3, 13);

        //생성된 토마토의 side stem과 연결될 main stem의 위치
        froms = new Vector3[7];
        froms[0] = new Vector3(-33, -64, 126);
        froms[1] = new Vector3(88, -21, 67);
        froms[2] = new Vector3(-4, 55, 66);
        froms[3] = new Vector3(-33, -67, 31);
        froms[4] = new Vector3(-2, 138, 102);
        froms[5] = new Vector3(11, 60, 150);
        froms[6] = new Vector3(-35, 13, 66);
        check = new int[] { 0, 0, 0, 0, 0, 0, 0}; //같은 위치에 중복하여 토마토가 배치되지 않도록 하기 위한 check

        for (int k = 0; k < tomato_list.Length; k++)
        {
            tomato_list[k].active = false;
        }
        
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
        
        /************1.plant의 position, rotaion을 random하게 설정************/
        trans = new Vector3(Random.Range(-60f, 60f), Random.Range(-100f, -60f), Random.Range(150f, 250f));
        plant.transform.position = trans;
        rotate_vector = new Vector3(Random.Range(250f, 270f), Random.Range(0f, 90f), Random.Range(0f, 90f));
        plant.transform.eulerAngles = rotate_vector;

        //background 설정
        if(background == null)
            tomato_camera.backgroundColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        
        //조명 설정
        //  light.intensity = Random.Range(3f, 15f);


        check = new int[] { 0, 0, 0, 0, 0, 0, 0};

        
        for (int k = 0; k < tomato_list.Length;k++)
        {
            GameObject tomato;
            GameObject self_plant;
            int count = 0;

            /***********2. annotate 대상 tomato 선택***********/
            while (k<tomato_list.Length-1 && tomato_list[k].name == tomato_list[k + 1].name)
            {
                k++;
                count++;
            }
            int tomato_idx = Random.Range(0, count+1);

            tomato = tomato_list[k - tomato_idx];
            self_plant = plant_list[k - tomato_idx];
            

            if (Random.Range(0f, 1f) < 0.5f)
            {
                tomato.active = true;
            }
            if (tomato.name == "0") tomato.active = true;

            int idx;
            while (true)
            {
                idx = Random.Range(0, 7);
                
                if(check[idx]==0)
                {
                    check[idx] = 1;
                    break;
                }
            }

            /***********3. plant 주변에 tomato를 현실과 유사하게 배치***********/
            tomato.transform.position = trans + Matrix4x4.Rotate(Quaternion.Euler(rotate_vector)).rotation*(spots[idx]+new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), Random.Range(-50f,-20f)));
            tomato.transform.rotation = Quaternion.Euler(new Vector3(plant.transform.eulerAngles.x+Random.Range(-20f,20f), plant.transform.eulerAngles.y+Random.Range(-20f, 20f), plant.transform.eulerAngles.z+ Random.Range(-20f, 20f)));
            
            //stem을 main plant에 연결
            self_plant.transform.position = tomato.transform.GetChild(2).position;
            var plant_scale = self_plant.transform.localScale;
            plant_scale.z = (trans + Matrix4x4.Rotate(Quaternion.Euler(rotate_vector)).rotation * (froms[idx])-(tomato.transform.GetChild(2).position)).magnitude / 2.0f;
            plant_scale.z = plant_scale.z * 0.05f;
            self_plant.transform.localScale = plant_scale;
            self_plant.transform.rotation = Quaternion.FromToRotation(Vector3.forward, trans + Matrix4x4.Rotate(Quaternion.Euler(rotate_vector)).rotation * (froms[idx]) - (tomato.transform.GetChild(2).position));

            /***********4. 카메라 기준 translation, rotation 계산***********/
            Transform tmp = tomato.transform;
            Vector3 tmp_rot = tmp.rotation.eulerAngles;

            Transform stem = self_plant.transform;
            Vector3 stem_rot = stem.localEulerAngles;

            /***********5. 오른손 좌표계 기준으로 변환***********/
            Matrix4x4 rot = Matrix4x4.Rotate(Quaternion.Euler(new Vector3(-tmp_rot.x, tmp_rot.y, -tmp_rot.z)));
            Matrix4x4 stem_rot_matrix = Matrix4x4.Rotate(Quaternion.Euler(new Vector3(-stem_rot.x, stem_rot.y, -stem_rot.z)));


            //직육면체 visualize(annotate와는 관련 없어 삭제해도 무방(but 삭제시 translationsxy2D를 위한 값은 남겨야 함)
            Bounds bounds = tomato.GetComponent<MeshFilter>().mesh.bounds;
            v3Center = bounds.center;
            Vector3 v3Extents = bounds.extents;
            //local points
            v3FrontTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top left corner
            v3FrontTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top right corner
            v3FrontBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom left corner
            v3FrontBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom right corner
            v3BackTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top left corner
            v3BackTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top right corner
            v3BackBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom left corner
            v3BackBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom right corner
            //world points
            v3Center = tomato.transform.TransformPoint(v3Center);
            v3FrontTopLeft = tomato.transform.TransformPoint(v3FrontTopLeft);
            v3FrontTopRight = tomato.transform.TransformPoint(v3FrontTopRight);
            v3FrontBottomLeft = tomato.transform.TransformPoint(v3FrontBottomLeft);
            v3FrontBottomRight = tomato.transform.TransformPoint(v3FrontBottomRight);
            v3BackTopLeft = tomato.transform.TransformPoint(v3BackTopLeft);
            v3BackTopRight = tomato.transform.TransformPoint(v3BackTopRight);
            v3BackBottomLeft = tomato.transform.TransformPoint(v3BackBottomLeft);
            v3BackBottomRight = tomato.transform.TransformPoint(v3BackBottomRight);
            DrawBox(k);
            points[0] = v3Center;
            points[0] = tomato_camera.WorldToScreenPoint(points[0]);
            points[0].y = 480 - points[0].y; // unity의 이미지 좌표계와 opencv의 이미지 좌표계 기준이 달라서 변환해준
            points[0].x = 640 - points[0].x;
                
            /***********6. annotate***********/
            if (tomato.active)
            {
                labels_writer.WriteLine(string.Format("{0}", tomato.name));

                rotations_writer.WriteLine(string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} 1 {9}", rot[0,0], rot[0, 1], rot[0, 2], rot[1, 0], rot[1, 1], rot[1, 2], rot[2, 0], rot[2, 1], rot[2, 2], tomato.name));

                stem_rotations_writer.WriteLine(string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8}", stem_rot_matrix[0, 0], stem_rot_matrix[0, 1], stem_rot_matrix[0, 2], stem_rot_matrix[1, 0], stem_rot_matrix[1, 1], stem_rot_matrix[1, 2], stem_rot_matrix[2, 0], stem_rot_matrix[2, 1], stem_rot_matrix[2, 2]));

                translations_writer.WriteLine(string.Format("{0} {1} {2}", tomato.transform.position.x, -tomato.transform.position.y, tomato.transform.position.z));

                translationsxy2D_writer.WriteLine(string.Format("{0} {1}", points[0].x, points[0].y));
               
            }
        }

        camk_writer.WriteLine("415 0. 320\n0. 415.57043 240\n0. 0. 1.");

        camk_writer.Close();
        labels_writer.Close();
        rotations_writer.Close();
        stem_rotations_writer.Close();
        translations_writer.Close();
        translationsxy2D_writer.Close();

        /***********7. 이미지 캡쳐***********/
        Screenshot(rgb_dir, tomato_camera, masks_dir, mask_camera);

        file_count++;
        
        for(int k = 0; k < tomato_list.Length; k++)
        {
            tomato_list[k].active = false;
        }
        
        sw.Stop();

        //UnityEngine.Debug.Log(sw.ElapsedMilliseconds.ToString() + "ms"); //annotate 속도 측정
    }

    public void Screenshot(string tomato_path, Camera tomato_camera,string mask_path,Camera mask_camera)
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

    void DrawBox(int k)
    {
            lineDrawer[k,0].DrawLineInGameView(v3FrontTopLeft, v3FrontTopRight, color);
            lineDrawer[k,1].DrawLineInGameView(v3FrontTopRight, v3FrontBottomRight, color);
            lineDrawer[k,2].DrawLineInGameView(v3FrontBottomRight, v3FrontBottomLeft, color);
            lineDrawer[k,3].DrawLineInGameView(v3FrontBottomLeft, v3FrontTopLeft, color);

            lineDrawer[k,4].DrawLineInGameView(v3BackTopLeft, v3BackTopRight, color);
            lineDrawer[k,5].DrawLineInGameView(v3BackTopRight, v3BackBottomRight, color);
            lineDrawer[k,6].DrawLineInGameView(v3BackBottomRight, v3BackBottomLeft, color);
            lineDrawer[k,7].DrawLineInGameView(v3BackBottomLeft, v3BackTopLeft, color);

            lineDrawer[k,8].DrawLineInGameView(v3FrontTopLeft, v3BackTopLeft, color);
            lineDrawer[k,9].DrawLineInGameView(v3FrontTopRight, v3BackTopRight, color);
            lineDrawer[k,10].DrawLineInGameView(v3FrontBottomRight, v3BackBottomRight, color);
            lineDrawer[k,11].DrawLineInGameView(v3FrontBottomLeft, v3BackBottomLeft, color);
        
    }

}
public struct LineDrawer
{
    private LineRenderer lineRenderer;
    private float lineSize;

    public LineDrawer(float lineSize = 0.2f)
    {
        GameObject lineObj = new GameObject("LineObj");
        lineRenderer = lineObj.AddComponent<LineRenderer>();
        //Particles/Additive
        lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));

        this.lineSize = lineSize;
    }

    private void init(float lineSize = 0.2f)
    {
        if (lineRenderer == null)
        {
            GameObject lineObj = new GameObject("LineObj");
            lineRenderer = lineObj.AddComponent<LineRenderer>();
            //Particles/Additive
            lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));

            this.lineSize = lineSize;
        }
    }

    //Draws lines through the provided vertices
    public void DrawLineInGameView(Vector3 start, Vector3 end, Color color)
    {
        if (lineRenderer == null)
        {
            init(0.2f);
        }

        //Set color
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        //Set width
        lineRenderer.startWidth = lineSize;
        lineRenderer.endWidth = lineSize;

        //Set line count which is 2
        lineRenderer.positionCount = 2;

        //Set the postion of both two lines
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    public void Destroy()
    {
        if (lineRenderer != null)
        {
            UnityEngine.Object.Destroy(lineRenderer.gameObject);
        }
    }
}