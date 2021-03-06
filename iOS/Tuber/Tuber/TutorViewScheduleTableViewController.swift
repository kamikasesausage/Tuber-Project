//
//  TutorViewScheduleTableViewController.swift
//  Tuber
//
//  Created by Anne on 12/7/16.
//  Copyright © 2016 Tuber. All rights reserved.
//

import UIKit

class TutorViewScheduleTableViewController: UITableViewController {
    
    let sections = ["Scheduled Appointments", "Appointment Requests"]
    
    // Set on previous screen (TutorClassListViewController or OfferTutorViewController)
    var students: [[String]] = []
    var dates: [[String]] = []
    var duration: [[String]] = []
    var subjects: [[String]] = []
    
    override func viewDidLoad() {
        super.viewDidLoad()

        self.title = UserDefaults.standard.object(forKey: "selectedCourse") as? String
        
        self.view.backgroundColor = UIColor(patternImage: #imageLiteral(resourceName: "background"))
        self.tableView.separatorStyle = .none
        
        self.navigationController?.navigationBar.isTranslucent = false
        self.navigationController?.willMove(toParentViewController: OfferTutorTableViewController())
        
//        self.navigationItem.hidesBackButton = true
//        let newBackButton = UIBarButtonItem(title: "< Back", style: UIBarButtonItemStyle.plain, target: self, action: #selector(TutorViewScheduleTableViewController.back(_:)))
//        self.navigationItem.leftBarButtonItem = newBackButton
    }
    
    override func tableView(_ tableView: UITableView, willDisplayHeaderView view: UIView, forSection section: Int){
        view.tintColor = UIColor.darkGray
        let header = view as! UITableViewHeaderFooterView
        header.textLabel?.textColor = UIColor.white
    }
    
    func back(_ sender: UIBarButtonItem) {
        navigationController!.popToViewController(navigationController!.viewControllers[1], animated: false)
    }

    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }

    // MARK: - Table view data source

    override func numberOfSections(in tableView: UITableView) -> Int {
        return 2
    }

    override func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        // TODO: DB query for appointments
        return students[section].count
    }
    
    override func tableView(_ tableView: UITableView, titleForHeaderInSection section: Int) -> String? {
        return sections[section]
    }
    
    
    override func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let cell = tableView.dequeueReusableCell(withIdentifier: "studentAppointments", for: indexPath) as! TutorViewScheduleTableViewCell
        
        cell.studentNameLabel.text = students[indexPath.section][indexPath.row]
        cell.dateLabel.text = dates[indexPath.section][indexPath.row]
        cell.durationLabel.text = duration[indexPath.section][indexPath.row]
        cell.subjectLabel.text = subjects[indexPath.section][indexPath.row]
        
        // Creates separation between cells
        cell.contentView.backgroundColor = UIColor(patternImage: #imageLiteral(resourceName: "background"))
        let whiteRoundedView : UIView = UIView(frame: CGRect(x: 10, y: 10, width: self.view.frame.size.width - 20, height: 115))
        whiteRoundedView.layer.backgroundColor = CGColor(colorSpace: CGColorSpaceCreateDeviceRGB(), components: [1.0, 1.0, 1.0, 1.0])
        whiteRoundedView.layer.masksToBounds = false
        whiteRoundedView.layer.cornerRadius = 3.0
        whiteRoundedView.layer.shadowOffset = CGSize(width: -1, height: 1)
        whiteRoundedView.layer.shadowOpacity = 0.5
        cell.contentView.addSubview(whiteRoundedView)
        cell.contentView.sendSubview(toBack: whiteRoundedView)
        
        return cell
    }
    
    override func tableView(_ tableView: UITableView, didSelectRowAt indexPath: IndexPath) {
        let indexPath = tableView.indexPathForSelectedRow //optional, to get from any UIButton for example
                
        let currentCell = tableView.cellForRow(at: indexPath!)! as! TutorViewScheduleTableViewCell
        
        selectedAppointment.studentName = currentCell.studentNameLabel.text!
        selectedAppointment.date = currentCell.dateLabel.text!
        selectedAppointment.duration = currentCell.durationLabel.text!
        selectedAppointment.subject = currentCell.subjectLabel.text!
        
        if (indexPath?.section == 0)
        {
            selectedAppointment.buttonLabel = "Start Session"
        }
        else
        {
            selectedAppointment.buttonLabel = "Accept Request"
        }
        
        performSegue(withIdentifier: "selectAppointment", sender: nil)
                
    }
    
    /**
     * This struct is used from the UnconfirmedAppointmentViewController
     */
    struct selectedAppointment {
        static var studentName = String()
        static var date = String()
        static var duration = String()
        static var subject = String()
        static var buttonLabel = String()
    }
}
