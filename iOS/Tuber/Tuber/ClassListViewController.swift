//
//  ClassListViewController.swift
//  Tuber
//
//  Created by Anne on 12/6/16.
//  Copyright © 2016 Tuber. All rights reserved.
//

import UIKit

class ClassListViewController: UIViewController, UITableViewDataSource, UITableViewDelegate, CAPSPageMenuDelegate {

    @IBOutlet weak var classTableView: UITableView!
    
    var pageMenu : CAPSPageMenu?
    
    var classes = UserDefaults.standard.object(forKey: "userStudentCourses") as! Array<String>
    
    override func viewDidLoad() {
        super.viewDidLoad()
    }

    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }

    func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        //TODO: DB query, how many classes enrolled
        return classes.count
    }
    
    func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = tableView.dequeueReusableCell(withIdentifier: "cell", for: indexPath) as! ClassTableViewCell
        
        //TODO: DB query,
        cell.classNameLabel.text = classes[indexPath.row]
        cell.messageButton.setImage(#imageLiteral(resourceName: "messaging"), for: .normal)
        cell.immediateButton.setImage(#imageLiteral(resourceName: "immediaterequest"), for: .normal)
        cell.scheduledButton.setImage(#imageLiteral(resourceName: "scheduletutor"), for: .normal)
        cell.hotspotButton.setImage(#imageLiteral(resourceName: "studyhotspot"), for: .normal)
        
        return cell
    }
    
    func tableView(_ tableView: UITableView, didSelectRowAt indexPath: IndexPath) {
        let indexPath = tableView.indexPathForSelectedRow //optional, to get from any UIButton for example
        let currentCell = tableView.cellForRow(at: indexPath!)! as! ClassTableViewCell
        UserDefaults.standard.set(currentCell.classNameLabel.text! as String?, forKey: "selectedCourse")
        selectedClass.className = currentCell.classNameLabel.text!
        performSegue(withIdentifier: "selectClass", sender: nil)
    }
    
    struct selectedClass {
        static var className = String()
    }
    
}
