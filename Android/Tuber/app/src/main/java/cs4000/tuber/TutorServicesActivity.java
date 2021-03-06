package cs4000.tuber;

import android.content.Intent;
import android.os.Bundle;
import android.app.Activity;
import android.util.Log;
import android.view.View;

/*
 * Displays a view for students in which tutor services are viewable
 */
public class TutorServicesActivity extends Activity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_tutor_services);
    }

    public void tutor_services_immediate_request(View view) {
        Log.i("ImmediateReq", getIntent().getStringExtra("course"));
        Intent intent = new Intent(TutorServicesActivity.this, ImmediateStudentRequestActivity.class);
        intent.putExtra("course", getIntent().getStringExtra("course"));
        startActivity(intent);
    }

    public void tutor_services_schedule_request(View view) {
        Log.i("ScheduelTutor", getIntent().getStringExtra("course"));
        Intent intent = new Intent(TutorServicesActivity.this, ScheduleATutor.class);
        intent.putExtra("course", getIntent().getStringExtra("course"));
        startActivity(intent);
    }

    public void view_scheduled_requests(View view) {
        Log.i("ViewScheduled", getIntent().getStringExtra("course"));
        Intent intent = new Intent(TutorServicesActivity.this, TutoringRequests.class);
        intent.putExtra("course", getIntent().getStringExtra("course"));
        startActivity(intent);
    }
}
