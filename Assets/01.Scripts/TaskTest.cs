using System.Collections;
using UnityEngine;
using System.Threading.Tasks;

public class TaskTest : MonoBehaviour
{
    // '비동기': '동기'의 반대말로 어떤 '작업'을 실행할 때 그 작업이 완료되지 않아도
    //           다음 코드를 실행하는 방식
    // 그 '작업'의 특징: 시간이 오래걸린다. (ex. 연산량이 많거나, IO 작업 등)

    public GameObject PatrolObject;
    
    private async void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space");
            // 1. 동기
            // LongLoop();
            
            // 2. 비동기 (Coroutine)
            // StartCoroutine(LongLoop_Coroutine());
            
            // 3. 비동기 (Task)
            // 3-1. 반환값이 없는 Task
            // Task task1 = new Task(LongLoop);
            // task1.Start();
            
            // 3-2. 반환값이 있는 Task
            Task<int> task2 = new Task<int>(LongLoop2);
            // task2.Start();

            // task2.Wait(); // task2 작업이 끝나기를 기다린다. (비동기를 강제로 동기로 만드는 함수, 즉 사용 X)
            // var result = task2.Result;
            // Debug.Log(result);

            // ContinueWith을 사용한 콜백 방식
            // task2.ContinueWith((t) =>
            // {
            //     int result = t.Result;
            //     Debug.Log(result);
            // });
            
            // 비동기 함수를 동기처럼 이해하기 쉽게 만드는 키워드가 async + await이다.
            var result = await task2;
            Debug.Log(result);
        }
    }
 

    // 연산량이 많은 작업
    private void LongLoop()
    {
        long sum = 1;
        for (long i = 0; i < 10000000000; ++i)
        {
            sum *= i;
        }
        
        Debug.Log("작업 완료");
        // Task를 이용한 호출에서 아래 MonoBehaviour를 상속받는 코드는 실행이 안될수도 있다.
        PatrolObject.SetActive(false);
    }
    
    private int LongLoop2()
    {
        long sum = 1;
        for (long i = 0; i < 10000000000; ++i)
        {
            sum *= i;
        }
        
        Debug.Log("작업 완료");
        return 213124;
    }
    
    private IEnumerator LongLoop_Coroutine()
    {
        long sum = 1;
        for (long i = 1; i < 10000000000; ++i) // 
        {
            sum *= i;
            if (i % 10000000 == 0)
            {
                Debug.Log(i);
                yield return null;
            }
        }
        Debug.Log("작업 완료");
    }
}