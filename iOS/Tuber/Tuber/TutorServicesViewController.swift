//
//  TutorServicesViewController.swift
//  Tuber
//
//  Created by Anne on 12/7/16.
//  Copyright © 2016 Tuber. All rights reserved.
//

import UIKit
import CoreLocation

class TutorServicesViewController: UIViewController, UITableViewDataSource, UITableViewDelegate,CLLocationManagerDelegate {

    @IBOutlet weak var servicesTableView: UITableView!

    var icons = [#imageLiteral(resourceName: "immediaterequest"), #imageLiteral(resourceName: "scheduletutor"), #imageLiteral(resourceName: "viewschedule"), #imageLiteral(resourceName: "studyhotspot"), #imageLiteral(resourceName: "messaging"), #imageLiteral(resourceName: "offertutor")]
    var names = ["Immediate Request", "Schedule Tutor", "View Schedule", "Study Hotspot", "Messaging", "Report Tutor"]
    
    
    let manager = CLLocationManager();
    
    var location:CLLocation?;
    var myLocation:CLLocationCoordinate2D?;
    var haveLocation = false;
    
    var returnedJSON: [String : AnyObject] = [:];
    var longitude: [Double] = [];
    var latitude: [Double] = [];
    
    func locationManager(_ manager: CLLocationManager, didUpdateLocations locations: [CLLocation])
    {
        self.location = locations[0]
        self.myLocation = CLLocationCoordinate2DMake(location!.coordinate.latitude, location!.coordinate.longitude)
    }

    override func viewDidLoad() {
        super.viewDidLoad()
        self.title = UserDefaults.standard.object(forKey: "selectedCourse") as? String
        
        self.navigationController?.navigationBar.isTranslucent = false
        servicesTableView.tableFooterView = UIView(frame: .zero)
        self.view.backgroundColor = UIColor(patternImage: #imageLiteral(resourceName: "background"))
        self.servicesTableView.separatorStyle = .none
        
        manager.delegate = self
        manager.desiredAccuracy = kCLLocationAccuracyBest
        manager.requestWhenInUseAuthorization()
        manager.startUpdatingLocation()
        
        let navArray:Array = (self.navigationController?.viewControllers)!

    }

    // Get rid of extra table cells
    override func viewDidAppear(_ animated: Bool) {
        servicesTableView.frame = CGRect(x: servicesTableView.frame.origin.x, y: servicesTableView.frame.origin.y, width: servicesTableView.frame.size.width, height: servicesTableView.contentSize.height)
    }
    
    // Get rid of extra table cells
    override func viewDidLayoutSubviews(){
        servicesTableView.frame = CGRect(x: servicesTableView.frame.origin.x, y: servicesTableView.frame.origin.y, width: servicesTableView.frame.size.width, height: servicesTableView.contentSize.height)
        servicesTableView.reloadData()
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    
    
    func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        return names.count
    }
    
    func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = tableView.dequeueReusableCell(withIdentifier: "tutorServices", for: indexPath) as! TutorServicesTableViewCell
        
        cell.optionIconImageView.image = icons[indexPath.row]
        cell.optionNameLabel.text = names[indexPath.row]
        
        //Creates separation between cells
        cell.contentView.backgroundColor = UIColor(patternImage: #imageLiteral(resourceName: "background"))
        let whiteRoundedView : UIView = UIView(frame: CGRect(x: 0, y: 10, width: self.view.frame.size.width - 35, height: 70))
        whiteRoundedView.layer.backgroundColor = CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [1.0, 1.0, 1.0, 1.0])
        whiteRoundedView.layer.masksToBounds = false
        whiteRoundedView.layer.cornerRadius = 3.0
        whiteRoundedView.layer.shadowOffset = CGSize(width: -1, height: 1)
        whiteRoundedView.layer.shadowOpacity = 0.5
        cell.contentView.addSubview(whiteRoundedView)
        cell.contentView.sendSubview(toBack: whiteRoundedView)
        
        return cell
    }
    
    /**
     * This fuction performs the correct segue based off of which cell was selected
     */
    func tableView(_ tableView: UITableView, didSelectRowAt indexPath: IndexPath) {
        let indexPath = tableView.indexPathForSelectedRow //optional, to get from any UIButton for example
        
        let currentCell = tableView.cellForRow(at: indexPath!)! as! TutorServicesTableViewCell
        
        let selectedOption = currentCell.optionNameLabel.text
        
        if selectedOption == "Immediate Request"
        {
            prepareImmediateTutorList()
        }
        else if selectedOption == "Schedule Tutor"
        {
            performSegue(withIdentifier: "scheduleTutor", sender: selectedOption)
        }
        else if selectedOption == "Study Hotspot"
        {
            checkHotspotStatus()
        }
        else if selectedOption == "View Schedule"
        {
            prepStudentSchedule()
        }
        else if selectedOption == "Messaging"
        {
            loadMessageUsers()
        }
        else{
            prepTutorList()
        }
    }
    
    /**
     * This fuction accesses the database to load all of the tutors the student can report.
     */
    func prepTutorList()
    {
        var tutorFirstNames: [String] = []
        var tutorLastNames: [String] = []
        var tutorEmails: [String] = []
        
        // Set up the post request
        let server = "http://tuber-test.cloudapp.net/ProductRESTService.svc/reporttutorgettutorlist";
        let requestURL = URL(string: server)
        let request = NSMutableURLRequest(url: requestURL! as URL)
        request.httpMethod = "POST"
        
        // Create the post parameters
        let defaults = UserDefaults.standard
        let email = defaults.object(forKey: "userEmail") as? String
        let token = defaults.object(forKey: "userToken") as? String
        let postParameters = "{\"userEmail\":\"" + email! + "\",\"userToken\":\"" + token! + "\"}";
        
        
        // Adding the parameters to request body
        request.httpBody = postParameters.data(using: String.Encoding.utf8)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("application/json", forHTTPHeaderField: "Accept")
        
        // Creating a task to send the post request
        let task = URLSession.shared.dataTask(with: request as URLRequest){
            data, response, error in
            
            if error != nil{
                print("error is \(error)")
                return;
            }
            
            // Parsing the response
            do {
                let tutors = try JSONSerialization.jsonObject(with: data!, options: JSONSerialization.ReadingOptions.allowFragments) as! [String : AnyObject]
                
                if let arrJSON = tutors["tutorList"] {
                    if (arrJSON.count > 0) {
                        for index in 0...arrJSON.count-1 {
                            
                            let aObject = arrJSON[index] as! [String : AnyObject]
                            
                            print(aObject)
                            
                            var name = aObject["tutorFirstName"] as! String
                            name += " "
                            name += aObject["tutorLastName"] as! String
                            
                            tutorFirstNames.append(aObject["tutorFirstName"] as! String)
                            tutorLastNames.append(aObject["tutorLastName"] as! String)
                            tutorEmails.append(aObject["tutorEmail"] as! String)
                            
                        }
                    }
                }
                
                OperationQueue.main.addOperation{
                    
                    // Set up the sender for the segue
                    var toSend = [[String]]()
                    toSend.append(tutorFirstNames)
                    toSend.append(tutorLastNames)
                    toSend.append(tutorEmails)
                    
                    self.performSegue(withIdentifier: "reportTutor", sender: toSend)
                }
            } catch {
                print(error)
            }
            
        }
        // Executing the task
        task.resume()
    }

    /**
     * This fuction accesses the database to load the user's scheduled tutor requests, accepted and not accepted.
     */
    func prepStudentSchedule()
    {
        // Set up the post request
        let requestURL = URL(string: "http://tuber-test.cloudapp.net/ProductRESTService.svc/checkscheduledpairedstatus")
        let request = NSMutableURLRequest(url: requestURL! as URL)
        request.httpMethod = "POST"
        
        // Create the post parameters
        let userEmail = UserDefaults.standard.object(forKey: "userEmail") as! String
        let userToken = UserDefaults.standard.object(forKey: "userToken") as! String
        let course = UserDefaults.standard.object(forKey: "selectedCourse") as! String
        let postParameters = "{\"userEmail\":\"" + userEmail + "\",\"userToken\":\"" + userToken + "\"}"
        
        // Adding the parameters to request body
        request.httpBody = postParameters.data(using: String.Encoding.utf8)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("application/json", forHTTPHeaderField: "Accept")
        
        
        // Creating a task to send the post request
        let task = URLSession.shared.dataTask(with: request as URLRequest){
            data, response, error in
            
            if error != nil{
                print(error as! String)
                return;
            }
            
            // Parsing the response
            do {
                let requests = try JSONSerialization.jsonObject(with: data!, options: JSONSerialization.ReadingOptions.allowFragments) as! [String : AnyObject]
                
                var dates = [[String](),[String]()]
                var durations = [[String](),[String]()]
                var topics = [[String](),[String]()]
                
                if let arrJSON = requests["requests"] {
                    if (arrJSON.count > 0)
                    {
                        for index in 0...arrJSON.count-1 {
                            
                            let aObject = arrJSON[index] as! [String : AnyObject]
                            
                            if ((aObject["course"] as! String) == course){
                                if (aObject["isPaired"] as! Bool == true){
                                    dates[0].append(aObject["dateTime"] as! String)
                                    durations[0].append(aObject["duration"] as! String)
                                    topics[0].append(aObject["topic"] as! String)
                                }
                                else{
                                    dates[1].append(aObject["dateTime"] as! String)
                                    durations[1].append(aObject["duration"] as! String)
                                    topics[1].append(aObject["topic"] as! String)
                                }
                                
                                
                            }
                        }
                        
                    }
                }
                OperationQueue.main.addOperation{

                    // Set up the sender for the segue
                    var toSend = [[[String]]]()
                    toSend.append(dates)
                    toSend.append(durations)
                    toSend.append(topics)
                    
                    self.performSegue(withIdentifier: "studentViewSchedule", sender: toSend)
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
     * This fuction accesses the database to load all of the users for the message list
     */
    func loadMessageUsers() {
        
        var emails: [String] = []
        var firstNames: [String] = []
        var lastNames: [String] = []
        
        // Set up the post request
        let requestURL = URL(string: "http://tuber-test.cloudapp.net/ProductRESTService.svc/getusers")
        let request = NSMutableURLRequest(url: requestURL! as URL)
        request.httpMethod = "POST"
        
        // Create the post parameters
        let defaults = UserDefaults.standard
        let userEmail = defaults.object(forKey: "userEmail") as! String
        let userToken = defaults.object(forKey: "userToken") as! String
        
        let postParameters = "{\"userEmail\":\"\(userEmail)\",\"userToken\":\"\(userToken)\"}"
        
        // Adding the parameters to request body
        request.httpBody = postParameters.data(using: String.Encoding.utf8)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("application/json", forHTTPHeaderField: "Accept")
        
        
        // Creating a task to send the post request
        let task = URLSession.shared.dataTask(with: request as URLRequest){
            data, response, error in
            
            if error != nil{
                print("error is \(error)")
                return;
            }
            
            // Parsing the response
            do {
                let messageUsers = try JSONSerialization.jsonObject(with: data!, options: JSONSerialization.ReadingOptions.allowFragments) as! [String : AnyObject]
                
                if let arrJSON = messageUsers["users"] {
                    if (arrJSON.count > 0) {
                        for index in 0...arrJSON.count-1 {
                            
                            let aObject = arrJSON[index] as! [String : AnyObject]
                            
                            emails.append(aObject["email"] as! String)
                            firstNames.append(aObject["firstName"] as! String)
                            lastNames.append(aObject["lastName"] as! String)
                            
                        }
                    }
                }
                
                OperationQueue.main.addOperation{
                    
                    // Set up the sender for the segue
                    var toSend = [[String]]()
                    
                    toSend.append(emails)
                    toSend.append(firstNames)
                    toSend.append(lastNames)
                    
                    self.performSegue(withIdentifier: "messages", sender: toSend)
                }
            } catch {
                print(error)
            }
            
        }
        // Executing the task
        task.resume()
    }

    override func prepare(for segue: UIStoryboardSegue, sender: Any?) {
        if segue.identifier == "studentViewSchedule"
        {
            let appointmentInfo = sender as! [[[String]]]
            
            if let destination = segue.destination as? StudentViewScheduleTableViewController
            {
                destination.dates = appointmentInfo[0]
                destination.duration = appointmentInfo[1]
                destination.subjects = appointmentInfo[2]
            }
        }
        else if segue.identifier == "reportTutor"
        {
            let tutorInfo = sender as! [[String]]

            
            if let destination = segue.destination as? ReportTutorListTableViewController
            {
                destination.tutorFirstNames = tutorInfo[0]
                destination.tutorLastNames = tutorInfo[1]
                destination.tutorEmails = tutorInfo[2]
            }
        }
        else if segue.identifier == "messages"
        {
            let appointmentInfo = sender as! [[String]]
            
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
        else if segue.identifier == "immediateRequest"
        {
            let tutorInfo = sender as! [[String]]
            
            
            if let destination = segue.destination as? ReportTutorListTableViewController
            {
                destination.tutorFirstNames = tutorInfo[0]
                destination.tutorLastNames = tutorInfo[1]
                destination.tutorEmails = tutorInfo[2]
            }
        }
        if segue.identifier == "activeHotspot"
        {
            let hotspotInfo = sender as! [String]
            
            if let destination = segue.destination as? ActiveHotspotViewController
            {
                destination.hotspotID = hotspotInfo[0]
                destination.messageContents = hotspotInfo[1]
                destination.pageSetup = hotspotInfo [2]
            }
        }
    }
    
    func prepareImmediateTutorList()
    {
        
        var tutorFirstNames: [String] = [];
        var tutorLastNames: [String] = [];
        var tutorEmails: [String] = [];
        
        let server = "http://tuber-test.cloudapp.net/ProductRESTService.svc/findavailabletutors";
        
        let requestURL = URL(string: server)
        
        let request = NSMutableURLRequest(url: requestURL! as URL)
        
        request.httpMethod = "POST"
        
        let defaults = UserDefaults.standard
        
        let userEmail = defaults.object(forKey: "userEmail") as! String
        let userToken = defaults.object(forKey: "userToken") as! String
        let course = defaults.object(forKey: "selectedCourse") as! String
        
        // let postParameters = "{\"userEmail\":\"\(userEmail)\",\"userToken\":\"\(userToken)\"tutorCourse\":\"  \(course)\"latitude\":\"  \(self.location!.coordinate.latitude)\"longitude\":\(self.location!.coordinate.longitude)\"}";
        let postParameters = "{\"userEmail\":\"\(userEmail)\",\"userToken\":\"\(userToken)\",\"course\":\"\(course)\",\"latitude\":\"\(self.location!.coordinate.latitude)\",\"longitude\":\"\(self.location!.coordinate.longitude)\"}"
        request.httpBody = postParameters.data(using: String.Encoding.utf8)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("application/json", forHTTPHeaderField: "Accept")
        
        print(postParameters)
        
        let task = URLSession.shared.dataTask(with: request as URLRequest)
        {
            data, response, error in
            
            if error != nil
            {
                print("error is \(error)")
                return;
            }
            
            let r = response as? HTTPURLResponse
            print(r?.statusCode)
            
            do
            {
                let tutors = try JSONSerialization.jsonObject(with: data!, options: JSONSerialization.ReadingOptions.allowFragments) as! [String : AnyObject]
                
                //self.returnedJSON = hotspots["studyHotspots"] as! [String : AnyObject]{
                if let arrJSON = tutors["tutorList"]
                {
                    if (arrJSON.count > 0)
                    {
                        for index in 0...arrJSON.count-1
                        {
                            
                            let aObject = arrJSON[index] as! [String : AnyObject]
                            
                            print(aObject)
                            
                            var name = aObject["tutorFirstName"] as! String
                            name += " "
                            name += aObject["tutorLastName"] as! String
                            
                            tutorFirstNames.append(aObject["tutorFirstName"] as! String)
                            tutorLastNames.append(aObject["tutorLastName"] as! String)
                            tutorEmails.append(aObject["tutorEmail"] as! String)
                            
                        }
                    }
                }
                OperationQueue.main.addOperation
                    {
                    
                        // Set up the sender for the segue
                        var toSend = [[String]]()
                        toSend.append(tutorFirstNames)
                        toSend.append(tutorLastNames)
                        toSend.append(tutorEmails)
                        
                    self.performSegue(withIdentifier: "immediateRequest", sender: toSend)
                }
                
                return;

            }
            catch
            {
                print(error)
            }
        }
        //executing the task
        task.resume()
    }

    func checkHotspotStatus()
    {
        var hotspot = Bool()
        var hotspotID = String()
        var course = String()
        var topic = String()
        var location = String()
        var ownerString = String()
        var owner = Bool()
        
        // Set up the post request
        let requestURL = URL(string: "http://tuber-test.cloudapp.net/ProductRESTService.svc/userhotspotstatus")
        let request = NSMutableURLRequest(url: requestURL! as URL)
        request.httpMethod = "POST"
        
        // Create the post parameters
        let defaults = UserDefaults.standard
        let userEmail = defaults.object(forKey: "userEmail") as! String
        let userToken = defaults.object(forKey: "userToken") as! String
        
        let postParameters = "{\"userEmail\":\"\(userEmail)\",\"userToken\":\"\(userToken)\"}"
        
        // Adding the parameters to request body
        request.httpBody = postParameters.data(using: String.Encoding.utf8)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("application/json", forHTTPHeaderField: "Accept")
        
        
        // Creating a task to send the post request
        let task = URLSession.shared.dataTask(with: request as URLRequest){
            data, response, error in
            
            if error != nil{
                print("error is \(error)")
                return;
            }
            
            // Parsing the response
            do {
                if let hotspotInfo = try JSONSerialization.jsonObject(with: data!, options: .allowFragments) as? NSDictionary{
                    
                    if let arrJSON = (hotspotInfo as AnyObject)["hotspot"] as? NSDictionary{
                        hotspot = true
                        
                        hotspotID = arrJSON["hotspotID"] as! String
                        course = arrJSON["course"] as! String
                        topic = arrJSON["topic"] as! String
                        location = arrJSON["locationDescription"] as! String
                        ownerString = arrJSON["ownerEmail"] as! String
                        
                        if (ownerString == userEmail)
                        {
                            owner = true
                        }
                        else
                        {
                            owner = false
                        }
                    }
                    
                }
                else
                {
                    hotspot = false
                }
                
                OperationQueue.main.addOperation{
                    
                    if(hotspot)
                    {
                        // Set up the sender for the segue
                        var toSend = [String]()
                        
                        toSend.append(hotspotID)
                        toSend.append("You are in a \(course) hotspot.")
                        
                        if (owner)
                        {
                            toSend.append("deleteCurrent")
                        }
                        else
                        {
                            toSend.append("leave")
                        }
                        self.performSegue(withIdentifier: "activeHotspot", sender: toSend)
                    }
                    else
                    {
                        self.performSegue(withIdentifier: "studyHotspot", sender: nil)
                    }
                }
            } catch {
                print(error)
            }
            
        }
        // Executing the task
        task.resume()
    }
}
