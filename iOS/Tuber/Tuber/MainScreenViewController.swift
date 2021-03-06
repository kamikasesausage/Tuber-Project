//
//  MainScreenViewController.swift
//  Tuber
//
//  Created by Anne on 4/15/17.
//  Copyright © 2017 Tuber. All rights reserved.
//

import UIKit

class MainScreenViewController: UIViewController {
    
    var pageMenu : CAPSPageMenu?
    
    override func viewDidLoad() {
        super.viewDidLoad()
    }
    
    override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)
        
        // MARK: Navigation bar setup
        self.title = "TUBER"
        self.navigationController?.navigationBar.barTintColor = UIColor(red: 62/255, green: 62/255, blue: 62/255, alpha: 1.0)
        self.navigationController?.navigationBar.shadowImage = UIImage()
        self.navigationController?.navigationBar.setBackgroundImage(UIImage(), for: UIBarMetrics.default)
        self.navigationController?.navigationBar.barStyle = UIBarStyle.default
        self.navigationController?.navigationBar.tintColor = UIColor.white //sidebuttons
        self.navigationController?.navigationBar.titleTextAttributes = [NSForegroundColorAttributeName: UIColor.orange]
        let backButton = UIBarButtonItem(title: "", style: UIBarButtonItemStyle.plain, target: navigationController, action: nil)
        navigationItem.leftBarButtonItem = backButton
        self.navigationController?.navigationBar.isTranslucent = false
        
        // MARK: - Scroll menu setup
        
        // Initialize view controllers to display and place in array
        var controllerArray : [UIViewController] = []
        
        let controller1 = storyboard?.instantiateViewController(withIdentifier: "ClassList")
        controller1?.title = "STUDENT CLASSES"
        controllerArray.append(controller1!)
        
        let controller2 = storyboard?.instantiateViewController(withIdentifier: "TutorClassList")
        controller2?.title = "TUTOR CLASSES"
        controllerArray.append(controller2!)
        
        // Customize menu
        let parameters: [CAPSPageMenuOption] = [
            .scrollMenuBackgroundColor(UIColor.darkGray), //color of scrollmenu
            .viewBackgroundColor(UIColor.black), //color of extra backgroud of the views
            .selectionIndicatorColor(UIColor.orange),
            .bottomMenuHairlineColor(UIColor.lightGray), //separation between page menu and view
            .menuItemFont(UIFont(name: "HelveticaNeue", size: 13.0)!),
            .menuHeight(40.0),
            .menuItemWidth(130.0),
            .centerMenuItems(true),
            .addBottomMenuShadow(true),
            .selectedMenuItemLabelColor(UIColor.white),
            //            .menuShadowColor(UIColor.blue),
            .menuShadowRadius(4)
        ]
        
        // Initialize scroll menu
        let rect = CGRect(origin: CGPoint(x: 0,y :0), size: CGSize(width: self.view.frame.width, height: self.view.frame.height))
        pageMenu = CAPSPageMenu(viewControllers: controllerArray, frame: rect, pageMenuOptions: parameters)
        
        
        self.addChildViewController(pageMenu!)
        self.view.addSubview(pageMenu!.view)
        
        pageMenu!.didMove(toParentViewController: self)
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    
    func didTapGoToLeft() {
        let currentIndex = pageMenu!.currentPageIndex
        
        if currentIndex > 0 {
            pageMenu!.moveToPage(currentIndex - 1)
        }
    }
    
    func didTapGoToRight() {
        let currentIndex = pageMenu!.currentPageIndex
        
        if currentIndex < pageMenu!.controllerArray.count {
            pageMenu!.moveToPage(currentIndex + 1)
        }
    }
    
    override func shouldAutomaticallyForwardRotationMethods() -> Bool {
        return true
    }
    
}

