//
//  HotspotInitialViewController.swift
//  Tuber
//
//  Created by Anne on 2/2/17.
//  Copyright © 2017 Tuber. All rights reserved.
//

import UIKit
import MapKit
import CoreLocation

class HotspotInitialViewController: UIViewController, CLLocationManagerDelegate {

    @IBOutlet weak var mapview: MKMapView!
    
    let manager = CLLocationManager();
    
    var location:CLLocation?
    var myLocation:CLLocationCoordinate2D?
    var haveLocation = false
    
    var returnedJSON: [String : AnyObject] = [:]
    var names: [String] = []
    var contacts: [String] = []
    
    func locationManager(_ manager: CLLocationManager, didUpdateLocations locations: [CLLocation]) {
        self.location = locations[0]
        
//        print(String(location!.coordinate.latitude))
//        print(String(location!.coordinate.longitude))
        
        let span:MKCoordinateSpan = MKCoordinateSpanMake(0.1, 0.1)
//        let myLocation:CLLocationCoordinate2D = CLLocationCoordinate2DMake(location!.coordinate.latitude, location!.coordinate.longitude)
        self.myLocation = CLLocationCoordinate2DMake(location!.coordinate.latitude, location!.coordinate.longitude)

        let region:MKCoordinateRegion = MKCoordinateRegionMake(self.myLocation!, span)
        
        mapview.setRegion(region, animated: true)
        
        self.mapview.showsUserLocation = true
        
        if (!haveLocation)
        {
            findHotspots(latitude: String(location!.coordinate.latitude), longitude: String(location!.coordinate.longitude))
            self.haveLocation = true
        }
    }
        
    override func viewDidLoad() {
        super.viewDidLoad()
        
        // Do any additional setup after loading the view.
        
        manager.delegate = self
        manager.desiredAccuracy = kCLLocationAccuracyBest
        manager.requestWhenInUseAuthorization()
        manager.startUpdatingLocation()
//        mapview.showsUserLocation = true
//        ring(describing: myLocation?.longitude))
        
//        findHotspots()
//        
//        print(returnedJSON.count)
//        
//        var annotations = [MKPointAnnotation]()
//
//        let annotation1 = MKPointAnnotation()
//        annotation1.coordinate = CLLocationCoordinate2DMake(37.8, -122.406417)
//        annotation1.title = names[0]
//        annotation1.subtitle = contacts[0]
//        
//        let annotation2 = MKPointAnnotation()
//        annotation2.coordinate = CLLocationCoordinate2DMake(37.77, -122.406417)
//        annotation1.title = names[1]
//        annotation1.subtitle = contacts[1]
//        
//        annotations.append(annotation1)
//        annotations.append(annotation2)
//        
//        mapview.addAnnotations(annotations)
    }

    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    

    @IBAction func createNewHotspot(_ sender: Any) {
    }
    
    
    func findHotspots(latitude: String, longitude: String)
    {
        let server = "http://tuber-test.cloudapp.net/ProductRESTService.svc/findstudyhotspots"
        
        //created NSURL
        let requestURL = NSURL(string: server)
        
        //creating NSMutableURLRequest
        let request = NSMutableURLRequest(url: requestURL! as URL)
        
        //setting the method to post
        request.httpMethod = "POST"
        
        let defaults = UserDefaults.standard
        
        //getting values from text fields
        let userEmail = defaults.object(forKey: "userEmail") as! String
        let userToken = defaults.object(forKey: "userToken") as! String
        let course = defaults.object(forKey: "selectedCourse") as! String
//        let latitude = String(describing: location?.coordinate.latitude)
//        let longitude = String(describing: location?.coordinate.longitude)
        
        //creating the post parameter by concatenating the keys and values from text field
        let postParameters = "{\"userEmail\":\"\(userEmail)\",\"userToken\":\"\(userToken)\",\"course\":\"\(course)\",\"latitude\":\"\(latitude)\",\"longitude\":\"\(longitude)\"}"
        
        //adding the parameters to request body
        request.httpBody = postParameters.data(using: String.Encoding.utf8)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("application/json", forHTTPHeaderField: "Accept")
        
        print(postParameters)
        
        //creating a task to send the post request
        let task = URLSession.shared.dataTask(with: request as URLRequest){
            data, response, error in
            
            if error != nil{
                print("error is \(error)")
                return;
            }
            
            //parsing the response
            do {
                //print(response)
                let hotspots = try JSONSerialization.jsonObject(with: data!, options: JSONSerialization.ReadingOptions.allowFragments) as! [String : AnyObject]
                
                //self.returnedJSON = hotspots["studyHotspots"] as! [String : AnyObject]{
                if let arrJSON = hotspots["studyHotspots"] {
                    for index in 0...arrJSON.count-1 {
                        
                        let aObject = arrJSON[index] as! [String : AnyObject]
                        
                        print(aObject)
                        
                        self.names.append(aObject["hotspotID"] as! String)
                        self.contacts.append(aObject["student_count"] as! String)
                        
                        print(aObject["hotspotID"] as! String)
                        print(aObject["student_count"] as! String)
                    }
                }
                print(self.names)
                print(self.contacts)
                
                //self.tableView.reloadData()
                //                //converting resonse to NSDictionary
                //                let myJSON =  try JSONSerialization.jsonObject(with: data!, options: .mutableContainers) as? NSDictionary
                //
                //                //parsing the json
                //                if let parseJSON = myJSON {
                //
                //                    //creating a string
                //                    var msg : String!
                //
                //                    //getting the json response
                //                    msg = parseJSON["message"] as! String?
                //
                //                    //printing the response
                //                    print(msg)
                //                    
                //                }
            } catch {
                print(error)
            }
            
        }
        //executing the task
        task.resume()
        
        while(self.names.isEmpty)
        {
            continue
        }
        
        print(self.names.count)
        print(self.contacts.count)
        
                var annotations = [MKPointAnnotation]()
        
                let annotation1 = MKPointAnnotation()
                annotation1.coordinate = CLLocationCoordinate2DMake(37.8, -122.406417)
                annotation1.title = names[0]
                annotation1.subtitle = contacts[0]
        
                let annotation2 = MKPointAnnotation()
                annotation2.coordinate = CLLocationCoordinate2DMake(37.77, -122.406417)
                annotation1.title = names[1]
                annotation1.subtitle = contacts[1]
        
                annotations.append(annotation1)
                annotations.append(annotation2)
                
                mapview.addAnnotations(annotations)
        
    }

}
