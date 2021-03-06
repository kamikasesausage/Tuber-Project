//
//  ServerRequest.swift
//  Tuber
//
//  Created by Hyunjin Cho on 2017. 2. 11..
//  Copyright © 2017년 Tuber. All rights reserved.
//

import Foundation;

class ServerRequest
{
    
    init()
    {
    }
    
    //IMPORTANT:pass in the JSON object as string!!
    func runRequest( inputJSON: String, server:String, completionHandler: @escaping (_ res:Int,_ JSON :NSDictionary) -> ())
    {
        //created NSURL
        let requestURL = URL(string: server)
        
        //creating NSMutableURLRequest
        let request = NSMutableURLRequest(url: requestURL! as URL)
        
        //setting the method to post
        request.httpMethod = "POST"
        
        //creating the post parameter by concatenating the keys and values from text field
                //adding the parameters to request body
        request.httpBody = inputJSON.data(using: String.Encoding.utf8)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("application/json", forHTTPHeaderField: "Accept")
        
        var myJSON:NSDictionary?;
        //creating a task to send the post request
        let task = URLSession.shared.dataTask(with: request as URLRequest)
        {
            data, response, error in
            
            if error != nil
            {
                print("error is \(error)")
                return;
            }
            
            let r = response as? HTTPURLResponse
            
            //parsing the response
            do {
                //converting resonse to NSDictionary
                myJSON =  try JSONSerialization.jsonObject(with: data!, options: .allowFragments) as? NSDictionary;
                completionHandler( (r?.statusCode)!,myJSON!);
                
            }
            catch {
                print(error)
            }        }
        //executing the task
        task.resume();

    }}
