package cs4000.tuber;

import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.preference.PreferenceManager;
import android.support.v7.app.AppCompatActivity;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import org.json.JSONException;
import org.json.JSONObject;

public class AvailableAcceptedRequestPage extends AppCompatActivity {

    private SharedPreferences sharedPreferences;
    private String _userEmail;
    private String _userToken;

    private String course;
    private String studentEmail;
    private String studentFirstName;
    private String studentLastName;
    private String topic;
    private String dateTime;
    private String duration;
    private String type;

    TextView studetEmailTextView;
    TextView courseTextView;
    TextView topicTextView;
    TextView dateTimeTextView;
    TextView durationTextView;
    Button sessionButton;

    public static AvailableAcceptedRequestPage getInstance(){
        return activity;
    }
    static AvailableAcceptedRequestPage activity;

    boolean acceptedRequest = false;

    Intent intent;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_available_request_page);
        setTitle("Open Request");

        intent = getIntent();
        activity = this;

        sharedPreferences = PreferenceManager.getDefaultSharedPreferences(this);
        _userEmail = sharedPreferences.getString("userEmail", "");
        _userToken = sharedPreferences.getString("userToken", "");

        course = intent.getStringExtra("course");
        Log.i("@course_check",course);
        studentEmail = intent.getStringExtra("studentEmail");
        studentFirstName = intent.getStringExtra("studentFirstName");
        studentLastName = intent.getStringExtra("studentLastName");
        topic = intent.getStringExtra("topic");
        dateTime = intent.getStringExtra("dateTime");
        duration = intent.getStringExtra("duration");
        type = intent.getStringExtra("type");

        studetEmailTextView = (TextView) findViewById(R.id.studentNameTextValue);
        courseTextView = (TextView) findViewById(R.id.courseTextValue2);
        topicTextView = (TextView) findViewById(R.id.topicTextValue2);
        dateTimeTextView = (TextView) findViewById(R.id.dateTimeTextValue2);
        durationTextView = (TextView) findViewById(R.id.durationTextvalue2);
        sessionButton = (Button) findViewById(R.id.sessionButton2);


        studetEmailTextView.setText(studentFirstName + " " + studentLastName);
        courseTextView.setText(course);
        topicTextView.setText(topic);
        dateTimeTextView.setText(dateTime);
        durationTextView.setText(duration);

        sessionButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {

                if(!acceptedRequest){

                    if(!_userEmail.equals(studentEmail)){

                        JSONObject obj = new JSONObject();
                        try {
                            obj.put("userEmail", _userEmail);
                            obj.put("userToken", _userToken);
                            obj.put("studentEmail", studentEmail);
                            obj.put("course", course);
                        } catch (JSONException e) {
                            e.printStackTrace();
                        }
                        ConnectionTask task = new ConnectionTask(obj);
                        task.accept_student_scheduled_request(new ConnectionTask.CallBack() {
                            @Override
                            public void Done(JSONObject result) {

                                if (result != null) {

                                    Toast.makeText(AvailableAcceptedRequestPage.this, "You have accepted the resquest successfully. You can now view the session"
                                            , Toast.LENGTH_LONG).show();
                                    sessionButton.setText("VIEW SESSION");

                                    acceptedRequest = true;
                                } else {
                                    Toast.makeText(AvailableAcceptedRequestPage.this, "Something went wrong! Try again"
                                            , Toast.LENGTH_LONG).show();
                                }

                            }
                        });

                    } else {
                        Toast.makeText(AvailableAcceptedRequestPage.this, "You can't accept a request that belongs to you"
                                , Toast.LENGTH_LONG).show();
                    }
                } else {

                    JSONObject obj2 = new JSONObject();
                    try{
                        obj2.put("userEmail", _userEmail);
                        obj2.put("userToken", _userToken);
                    } catch (JSONException e) {
                        e.printStackTrace();
                    }
                    ConnectionTask check_session_status = new ConnectionTask(obj2);
                    check_session_status.check_session_status_tutor(new ConnectionTask.CallBack() {
                        @Override
                        public void Done(JSONObject result) {
                            if(result != null){

                                try {

                                    String status = result.getString("session_status");
                                    Intent intent2 = new Intent(AvailableAcceptedRequestPage.this, Studysession.class);
                                    if(status.equals("active")){ // only offered but looking to pair
                                        intent2.putExtra("status", "1");
                                    } else if (status.equals("pending")) { // session has ended
                                        intent2.putExtra("status", "2");
                                    } else {
                                        intent2.putExtra("status", "0");
                                    }

                                    intent2.putExtra("dateTime", dateTime);
                                    intent2.putExtra("from", "scheduling");
                                    intent2.putExtra("course", course);
                                    startActivity(intent2);
                                } catch (JSONException e) {
                                    e.printStackTrace();
                                }

                            } else {
                                Intent intent2 = new Intent(AvailableAcceptedRequestPage.this, Studysession.class);
                                intent2.putExtra("status", "0");
                                intent2.putExtra("dateTime", dateTime);
                                intent2.putExtra("from", "scheduling");
                                intent2.putExtra("course", course);
                                startActivity(intent2);
                            }
                        }
                    });

                }

            }
        });

        if(type.equals("accepted")){
            sessionButton.setText("VIEW SESSION");
            acceptedRequest = true;
        }
    }
}
