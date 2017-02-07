package cs4000.tuber;

import android.support.v7.widget.RecyclerView;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import java.util.ArrayList;

/**
 * Created by Ali on 2/7/2017.
 */


public class RecyclerAdapter extends RecyclerView.Adapter<RecyclerView.ViewHolder>{

    private ArrayList<RecyclerCourseObject> dataList;


    @Override
    public int getItemCount() {
        return dataList.size();
    }

    @Override
    public void onBindViewHolder(RecyclerView.ViewHolder viewHolder, int position) {
        RecyclerCourseObject ru = dataList.get(position);

        if(ru.type=="one")
        {
            //typecast
            DataViewHolder dataViewHolder=(DataViewHolder) viewHolder;

            dataViewHolder.title.setText(ru.course);
            dataViewHolder.subTitle.setText(ru.subTitle);

        }
        else
        {
            DataViewHolder2 dataViewHolder2=(DataViewHolder2) viewHolder;
            dataViewHolder2.title.setText(ru.course);
            dataViewHolder2.subTitle.setText(ru.subTitle);
        }

    }

    public RecyclerAdapter(ArrayList<RecyclerCourseObject> dataList)
    {
        this.dataList = dataList;
    }

    @Override
    public RecyclerView.ViewHolder onCreateViewHolder(ViewGroup viewGroup, int viewType) {

        View itemView;
        RecyclerView.ViewHolder viewHold;
        switch(viewType)
        {
            case 0:
                itemView = LayoutInflater.
                        from(viewGroup.getContext()).
                        inflate(R.layout.course_layout_linear, viewGroup, false);
                viewHold= new DataViewHolder(itemView);
                break;

            default:
                itemView = LayoutInflater.
                        from(viewGroup.getContext()).
                        inflate(R.layout.course_layout_grid, viewGroup, false);
                viewHold= new DataViewHolder2(itemView);
                break;
        }

        return viewHold;
    }

    @Override
    public int getItemViewType(int position) {
        //More to come
        if(dataList.get(position).type=="one")
        {
            return 0;
        }
        else
        {
            return 1;
        }

    }


    public static class DataViewHolder extends RecyclerView.ViewHolder {

        protected TextView title;
        protected TextView subTitle;

        public DataViewHolder(View v) {
            super(v);
            title =  (TextView) v.findViewById(R.id.iTitle);
            subTitle = (TextView)  v.findViewById(R.id.iSubTitle);
        }
    }

    public static class DataViewHolder2 extends RecyclerView.ViewHolder {

        protected TextView title;
        protected TextView subTitle;

        public DataViewHolder2(View v) {
            super(v);
            title =  (TextView) v.findViewById(R.id.uTitle);
            subTitle = (TextView)  v.findViewById(R.id.uSubTitle);
        }
    }
}
