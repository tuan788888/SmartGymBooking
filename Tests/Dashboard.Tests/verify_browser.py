from playwright.sync_api import sync_playwright
from pathlib import Path
import json
base='http://127.0.0.1:5187'
paths=['/AdminDashboard','/EmployeeDashboard','/PaymentManagement','/BookingManagement','/CheckInManagement','/CustomerManagement','/EmployeeManagement']
roles=[('admin@smartgym.vn','Admin@123',[1,0,1,1,1,1,1]),('employee01@smartgym.vn','Employee@123',[0,1,1,1,1,1,0]),('nguyenvana@gmail.com','Customer@123',[0,0,0,0,0,0,0])]
with sync_playwright() as p:
    browser=p.chromium.launch(channel='msedge',headless=True)
    for email,password,allowed in roles:
        context=browser.new_context(viewport={'width':1440,'height':1000})
        page=context.new_page()
        errors=[]
        page.on('pageerror',lambda error: errors.append(str(error)))
        page.goto(base+'/Auth/Login')
        page.locator('#Email').fill(email)
        page.locator('#Password').fill(password)
        page.locator('button[type=submit]').click()
        page.wait_for_load_state('networkidle')
        for path,access in zip(paths,allowed):
            response=page.goto(base+path)
            page.wait_for_load_state('networkidle')
            if access:
                assert response.status==200 and '/Auth/AccessDenied' not in page.url,(email,path,page.url)
            else:
                assert '/Auth/AccessDenied' in page.url,(email,path,page.url)
        if email.startswith('admin'):
            page.goto(base+'/AdminDashboard')
            page.wait_for_function("typeof Chart !== 'undefined' && Chart.getChart('revenueChart') && Chart.getChart('bookingChart')")
            chart=page.evaluate("({revenue: Chart.getChart('revenueChart').data, bookings: Chart.getChart('bookingChart').data})")
            assert len(chart['revenue']['labels'])==6
            assert len(chart['bookings']['datasets'][0]['data'])==4
            Path('obj/dashboard-chart-data.json').write_text(json.dumps(chart),encoding='utf-8')
            page.screenshot(path='obj/admin-dashboard-desktop.png',full_page=True)
            page.set_viewport_size({'width':390,'height':844})
            page.wait_for_timeout(500)
            assert page.evaluate('document.documentElement.scrollWidth <= window.innerWidth + 1'),'Admin horizontal overflow'
            page.screenshot(path='obj/admin-dashboard-mobile.png',full_page=True)
        if email.startswith('employee'):
            page.goto(base+'/EmployeeDashboard')
            page.set_viewport_size({'width':390,'height':844})
            page.wait_for_timeout(500)
            assert page.evaluate('document.documentElement.scrollWidth <= window.innerWidth + 1'),'Employee horizontal overflow'
            page.screenshot(path='obj/employee-dashboard-mobile.png',full_page=True)
        assert not errors,errors
        context.close()
    browser.close()
print('PASS: all 21 role/URL checks, live Chart.js bar/doughnut instances, six months, mobile overflow and no browser JS errors.')
