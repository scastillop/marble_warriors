using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Google;
using UnityEngine.UI;
using Firebase.Auth;

public class googleSignIn : MonoBehaviour
{
    private FirebaseAuth auth;
    private FirebaseUser FBuser;
    public Text Info;
    // Start is called before the first frame update
    void Start()
    {
        InitializeFirebase();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void InitializeFirebase()
    {
        Debug.Log("Setting up Firebase Auth");
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                //   app = Firebase.FirebaseApp.DefaultInstance;

                // Set a flag here to indicate whether Firebase is ready to use by your app.
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
    }

    // Track state changes of the auth object.
    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != null)
        {
            bool signedIn = FBuser != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && FBuser != null)
            {
                Debug.Log("Signed out " + FBuser.UserId);
                Info.text = "sign out " + FBuser.UserId.ToString();
            }
            FBuser = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + FBuser.UserId);
                Info.text = "sign in " + FBuser.UserId.ToString();


            }
        }
    }



    public void googleSignInButton()
    {
        Info.text = "aprete el boton ";
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            RequestIdToken = true,
            // Copy this value from the google-service.json file.
            // oauth_client with type == 3
            WebClientId = "417951402462-7kpcf73dvrfjuot6j5gjiv3u0tsdtr9p.apps.googleusercontent.com"
        };
        Debug.Log(JsonUtility.ToJson(GoogleSignIn.Configuration, true));
        Info.text = "pase el client ";
        Task<GoogleSignInUser> signIn = GoogleSignIn.DefaultInstance.SignIn();

        TaskCompletionSource<FirebaseUser> signInCompleted = new TaskCompletionSource<FirebaseUser>();
        signIn.ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                signInCompleted.SetCanceled();
                Info.text = "canceled  1 " + FBuser.UserId.ToString();
            }
            else if (task.IsFaulted)
            {
                signInCompleted.SetException(task.Exception);
                Info.text = "is faulted 1 " + FBuser.UserId.ToString();
            }
            else
            {

                Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
                auth.SignInWithCredentialAsync(credential).ContinueWith(authTask =>
                {
                    if (authTask.IsCanceled)
                    {
                        signInCompleted.SetCanceled();
                        Info.text = "canceled  " + FBuser.UserId.ToString();
                    }
                    else if (authTask.IsFaulted)
                    {
                        signInCompleted.SetException(authTask.Exception);
                        Info.text = "is faulted  " + FBuser.UserId.ToString();
                    }
                    else
                    {
                        signInCompleted.SetResult(authTask.Result);
                        Info.text = "sign in  " + FBuser.UserId.ToString() + " " + FBuser.DisplayName.ToString();

                    }
                });
            }
        });
    }
}