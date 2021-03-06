//
//  TutorClassListViewController.swift
//  Tuber
//
//  Created by Anne on 4/22/17.
//  Copyright © 2017 Tuber. All rights reserved.
//

import UIKit

class TutorClassListViewController: UIViewController, UITableViewDataSource, UITableViewDelegate, ButtonCellDelegate {
    
    @IBOutlet weak var classTableView: UITableView!
    
    var classes = UserDefaults.standard.object(forKey: "userTutorCourses") as! Array<String>
    
    // Used in scheduledAppointments() and appointmentRequests()
    var studentNames: [[String]] = [[],[]]
    var dates: [[String]] = [[],[]]
    var durations: [[String]] = [[],[]]
    var topics: [[String]] = [[],[]]
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        classTableView.tableFooterView = UIView(frame: .zero)
        self.view.backgroundColor = UIColor(patternImage: #imageLiteral(resourceName: "background"))
        self.classTableView.separatorStyle = .none
    }
    
    // Get rid of extra table cells
    override func viewDidAppear(_ animated: Bool) {
        classTableView.frame = CGRect(x: classTableView.frame.origin.x, y: classTableView.frame.origin.y, width: classTableView.frame.size.width, height: classTableView.contentSize.height)        
    }
    
    // Get rid of extra table cells
    override func viewDidLayoutSubviews(){
        classTableView.frame = CGRect(x: classTableView.frame.origin.x, y: classTableView.frame.origin.y, width: classTableView.frame.size.width, height: classTableView.contentSize.height)
        classTableView.reloadData()
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    
    func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        return classes.count
    }
    
    func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = tableView.dequeueReusableCell(withIdentifier: "cell", for: indexPath) as! ClassTableViewCell
        
        cell.tutorClassNameLabel.text = classes[indexPath.row]
        cell.tutorClassNameLabel.font = UIFont(name: "HelveticaNeue", size: 28.0)!
        cell.tutorMessageButton.setImage(#imageLiteral(resourceName: "messaging"), for: .normal)
        cell.tutorImmediateButton.setImage(#imageLiteral(resourceName: "immediaterequest"), for: .normal)
        cell.tutorScheduledButton.setImage(#imageLiteral(resourceName: "scheduletutor"), for: .normal)
        
        if cell.buttonDelegate == nil {
            cell.buttonDelegate = self
        }
        
        // Creates separation between cells
        cell.contentView.backgroundColor = UIColor(patternImage: #imageLiteral(resourceName: "background"))
        let whiteRoundedView : UIView = UIView(frame: CGRect(x: 0, y: 10, width: self.view.frame.size.width - 35, height: 105))
        whiteRoundedView.layer.backgroundColor = CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [1.0, 1.0, 1.0, 1.0])
        whiteRoundedView.layer.masksToBounds = false
        whiteRoundedView.layer.cornerRadius = 3.0
        whiteRoundedView.layer.shadowOffset = CGSize(width: -1, height: 1)
        whiteRoundedView.layer.shadowOpacity = 0.5
        cell.contentView.addSubview(whiteRoundedView)
        cell.contentView.sendSubview(toBack: whiteRoundedView)
        
        return cell
    }
    
    func tableView(_ tableView: UITableView, didSelectRowAt indexPath: IndexPath) {
        let indexPath = tableView.indexPathForSelectedRow //optional, to get from any UIButton for example
        let currentCell = tableView.cellForRow(at: indexPath!)! as! ClassTableViewCell
        UserDefaults.standard.set(currentCell.tutorClassNameLabel.text! as String?, forKey: "selectedCourse")
        performSegue(withIdentifier: "tutorOptions", sender: nil)
    }
    
    // MARK: - ButtonCellDelegate
    /**
     * This fuction is called when a button in the table cell is called.  Allows the appropriate segue to be performed.
     */
    func cellTapped(cell: ClassTableViewCell, type: String) {
        
        let selectedCourse = classTableView.indexPath(for: cell)!.row
        UserDefaults.standard.set("\(classes[selectedCourse])", forKey: "selectedCourse")
        
        if (type == "Message"){
            loadMessageUsers()
        }
        else if (type == "Schedule"){
            scheduledAppointments()
        }
        else if (type == "Immediate"){
            performSegue(withIdentifier: "tutorImmediate", sender: nil)
        }
    }
    
    /**
     * This fuction accesses the database to load all of the users for the message list
     */
    func loadMessageUsers() {
        
        var emails: [String] = []
        var firstNames: [String] = []
        var lastNames: [String] = []
        
        //created NSURL
        let requestURL = URL(string: "http://tuber-test.cloudapp.net/ProductRESTService.svc/getusers")
        
        //creating NSMutableURLRequest
        let request = NSMutableURLRequest(url: requestURL! as URL)
        
        //setting the method to post
        request.httpMethod = "POST"
        
        let defaults = UserDefaults.standard
        
        let userEmail = defaults.object(forKey: "userEmail") as! String
        let userToken = defaults.object(forKey: "userToken") as! String
        
        //creating the post parameter by concatenating the keys and values from text field
        let postParameters = "{\"userEmail\":\"\(userEmail)\",\"userToken\":\"\(userToken)\"}"
        
        print(postParameters)
        
        //adding the parameters to request body
        request.httpBody = postParameters.data(using: String.Encoding.utf8)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("application/json", forHTTPHeaderField: "Accept")
        
        
        //creating a task to send the post request
        let task = URLSession.shared.dataTask(with: request as URLRequest){
            data, response, error in
            
            if error != nil{
                print("error is \(error)")
                return;
            }
            
            //parsing the response
            do {
                print(response)
                let hotspots = try JSONSerialization.jsonObject(with: data!, options: JSONSerialization.ReadingOptions.allowFragments) as! [String : AnyObject]
                
                if let arrJSON = hotspots["users"] {
                    if (arrJSON.count > 0) {
                        for index in 0...arrJSON.count-1 {
                            
                            let aObject = arrJSON[index] as! [String : AnyObject]
                            
                            print(aObject)
                            
                            
                            emails.append(aObject["email"] as! String)
                            firstNames.append(aObject["firstName"] as! String)
                            lastNames.append(aObject["lastName"] as! String)
                            
                        }
                    }
                }
                
                OperationQueue.main.addOperation{
                    
                    var toSend = [[String]]()
                    
                    toSend.append(emails)
                    toSend.append(firstNames)
                    toSend.append(lastNames)
                    
                    print(toSend.count)
                    
                    self.performSegue(withIdentifier: "messages", sender: toSend)
                }
            } catch {
                print(error)
            }
            
        }
        //executing the task
        task.resume()
    }
    
    /**
     * This fuction accesses the database to find the tutor's accepted scheduled appointments.
     */
    func scheduledAppointments()
    {
        
        // Set up the post request
        let requestURL = URL(string: "http://tuber-test.cloudapp.net/ProductRESTService.svc/findallscheduletutoracceptedrequests")
        let request = NSMutableURLRequest(url: requestURL! as URL)
        request.httpMethod = "POST"
        
        // Create the post parameters
        let userEmail = UserDefaults.standard.object(forKey: "userEmail") as! String
        let userToken = UserDefaults.standard.object(forKey: "userToken") as! String
        let course = UserDefaults.standard.object(forKey: "selectedCourse") as! String
        let postParameters = "{\"userEmail\":\"" + userEmail + "\",\"userToken\":\"" + userToken + "\",\"course\":\"" + course + "\"}"
        
        // Adding the parameters to request body
        request.httpBody = postParameters.data(using: String.Encoding.utf8)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("application/json", forHTTPHeaderField: "Accept")
        
        
        // Creating a task to send the post request
        let task = URLSession.shared.dataTask(with: request as URLRequest){
            data, response, error in
            
            if error != nil{
                return;
            }
            
            // Parsing the response
            do {
                let appointments = try JSONSerialization.jsonObject(with: data!, options: JSONSerialization.ReadingOptions.allowFragments) as! [String : AnyObject]
                
                self.studentNames = [[],[]]
                self.dates = [[],[]]
                self.durations = [[],[]]
                self.topics = [[],[]]
                
                if let arrJSON = appointments["tutorRequestItems"] {
                    print(arrJSON.count)
                    if (arrJSON.count > 0)
                    {
                        for index in 0...arrJSON.count-1 {
                            
                            let aObject = arrJSON[index] as! [String : AnyObject]
                            
                            self.dates[0].append(aObject["dateTime"] as! String)
                            self.durations[0].append(aObject["duration"] as! String)
                            self.studentNames[0].append(aObject["studentEmail"] as! String)
                            self.topics[0].append(aObject["topic"] as! String)
                        }
                        
                    }
                }

                // Find appointment requests
                OperationQueue.main.addOperation{
                    self.appointmentRequests()
                }
                
                return;
                
            } catch {
                print(error)
            }
        }
        // Executing the task
        task.resume()
    }
    
    /**
     * This fuction accesses the database to find scheduled appointment requests.
     */
    func appointmentRequests()
    {
        // Set up the post request
        let requestURL = URL(string: "http://tuber-test.cloudapp.net/ProductRESTService.svc/findallscheduletutorrequests")
        let request = NSMutableURLRequest(url: requestURL! as URL)
        request.httpMethod = "POST"
        
        // Create the post parameters
        let userEmail = UserDefaults.standard.object(forKey: "userEmail") as! String
        let userToken = UserDefaults.standard.object(forKey: "userToken") as! String
        let course = UserDefaults.standard.object(forKey: "selectedCourse") as! String
        let postParameters = "{\"userEmail\":\"" + userEmail + "\",\"userToken\":\"" + userToken + "\",\"course\":\"" + course + "\"}"
        
        // Adding the parameters to request body
        request.httpBody = postParameters.data(using: String.Encoding.utf8)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("application/json", forHTTPHeaderField: "Accept")
        
        // Creating a task to send the post request
        let task = URLSession.shared.dataTask(with: request as URLRequest){
            data, response, error in
            
            if error != nil{
                return;
            }
            
            let r = response as? HTTPURLResponse
            
            // Parsing the response
            do {
                let appointments = try JSONSerialization.jsonObject(with: data!, options: JSONSerialization.ReadingOptions.allowFragments) as! [String : AnyObject]
                
                if let arrJSON = appointments["tutorRequestItems"] {
                    print(arrJSON.count)
                    if (arrJSON.count > 0)
                    {
                        for index in 0...arrJSON.count-1 {
                            
                            let aObject = arrJSON[index] as! [String : AnyObject]
                            
                            self.dates[1].append(aObject["dateTime"] as! String)
                            self.durations[1].append(aObject["duration"] as! String)
                            self.studentNames[1].append(aObject["studentEmail"] as! String)
                            self.topics[1].append(aObject["topic"] as! String)
                            
                        }
                    }
                }

                OperationQueue.main.addOperation{
                    self.performSegue(withIdentifier: "tutorViewSchedule", sender: nil)
                }
                
                return;
                
            } catch {
                print(error)
            }
            
            
        }
        // Executing the task
        task.resume()
    }

    override func prepare(for segue: UIStoryboardSegue, sender: Any?) {
        if segue.identifier == "messages"
        {
            let appointmentInfo = sender as! [[String]]
            print(appointmentInfo[0])
            
            if let destination = segue.destination as? MessageUsersListViewController
            {
                destination.emails = []
                destination.firstNames = []
                destination.lastNames = []
                
                destination.emails = appointmentInfo[0]
                destination.firstNames = appointmentInfo[1]
                destination.lastNames = appointmentInfo[2]
            }
        }
        else if segue.identifier == "tutorViewSchedule"
        {
            if let destination = segue.destination as? TutorViewScheduleTableViewController
            {
                destination.students.removeAll()
                destination.dates.removeAll()
                destination.duration.removeAll()
                destination.subjects.removeAll()
                
                destination.students = self.studentNames
                destination.dates = self.dates
                destination.duration = self.durations
                destination.subjects = self.topics
            }
        }
    }

}
