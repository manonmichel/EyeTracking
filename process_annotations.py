import csv
import math
import statistics

sphere_lists = {
    't1_s1_1': [], 't1_s2_1': [], 't1_s3_1': [], 't1_s4_1': [], 't1_s5_1': [],
    't1_s6_1': [], 't1_s7_1': [], 't2_s2_1': [], 't2_s3_1': [], 't2_s4_1': [],
    't2_s5_1': [], 't2_s6_1': [], 't2_s7_1': [], 't1_s1_2': [], 't1_s2_2': [],
    't1_s3_2': [], 't1_s4_2': [], 't1_s5_2': [], 't1_s6_2': [], 't1_s7_2': [],
    't2_s2_2': [], 't2_s3_2': [], 't2_s4_2': [], 't2_s5_2': [], 't2_s6_2': [],
    't2_s7_2': [], 't1_s1_3': [], 't1_s2_3': [], 't1_s3_3': [], 't1_s4_3': [],
    't1_s5_3': [], 't1_s6_3': [], 't1_s7_3': [], 't2_s2_3': [], 't2_s3_3': [],
    't2_s4_3': [], 't2_s5_3': [], 't2_s6_3': [], 't2_s7_3': [],
}

averages = {}
stdevs = {}

def switch_sphere(switch, sphere, value):
    switch[sphere].append(value)

def average(lst):
    if(len(lst) == 0):
        return "N/A"
    else:
        return sum(lst) / len(lst)

def compute_stdev(lst):
    if(len(lst) < 2):
        return "N/A"
    else:
        return statistics.stdev(lst)

def emptyDictionary(dict):
    for key in dict.keys():
        dict[key] = []

# write a csv file at given path
def write_csv(path, file_name, dict, field):
    with open(path + 'post_processing/' + file_name +'.csv', 'w') as csv_file_out:
        csv_file_out.write("%s,%s\n"%('sphere_label',field))
        for name in dict.keys():
            csv_file_out.write("%s,%s\n"%(name,dict[name]))

# path: path to folder with recordings
# start: first id of these files
# end: last id of these files
def avg_per_sphere(path, start, end):
    id = start
    while True:
        with open(path + str(id) + '/exports/000/annotations.csv', mode='r') as csv_file_in:
            csv_reader = csv.DictReader(csv_file_in)
            line_count = 0
            emptyDictionary(sphere_lists)
            for row in csv_reader:
                #filter out certain values
                if(float(row['angularError'])<=5):
                    switch_sphere(sphere_lists, row['sphere_label'], float(row['angularError']))
                line_count += 1
            print(f'Processed {line_count} lines.')

        for name in sphere_lists.keys():
            averages[name] = average(sphere_lists[name])

        write_csv(path, 'avg_angularError_per_sphere_' + str(id), averages, 'avg_angularError')

        if(id == end):
            break
        else:
            id += 1

# path to folder with .csv files output by avg_per_sphere
# start: first id of these files
# end: last id of these files
def collective_stat(path, start, end):
    id = start
    emptyDictionary(sphere_lists)
    while True:
        with open(path + 'post_processing/avg_angularError_per_sphere_' + str(id) +'.csv', mode='r') as csv_file_in:
            csv_reader = csv.DictReader(csv_file_in)
            line_count = 0
            for row in csv_reader:
                #filter out certain values
                if(row['avg_angularError']!= "N/A"):
                    switch_sphere(sphere_lists, row['sphere_label'], float(row['avg_angularError']))
                line_count += 1
            print(f'Processed {line_count} lines.')

        if(id == end):
            break
        else:
            id += 1

    for name in sphere_lists.keys():
        averages[name] = average(sphere_lists[name])
        stdevs[name] = compute_stdev(sphere_lists[name])

    write_csv(path, 'collective_avg_sphere_' + str(start) +'_'+ str(end), averages, 'avg_angularError')
    write_csv(path, 'collective_stdev_sphere_' + str(start) +'_'+ str(end), stdevs, 'stdev_angularError')



# --------------- DATA VISUALISATION -------------- #
from tkinter import *

radius = 40


def circle_create(canv, xcoord, ycoord, label, lablis):
    # x1, y1, x2, y2
    canv.create_oval(xcoord - radius,ycoord-radius,xcoord+radius,ycoord+radius, outline="#011424", fill="#2F404E")
    circle_label = canv.create_text((xcoord, ycoord), text=label, fill="#FDFFFC", font="fixedsys 20")
    lablis.append(circle_label)
    return circle_label

# stat_file: .csv file output from collective_stat
# depth: desired depth to visualise
def visualise_data(stat_file, field, depth):
    if(depth!=1 and depth != 2 and depth !=3):
        sys.exit("Depth can only be 1,2 or 3")
    if(field != 'avg_angularError' and field != 'stdev_angularError'):
        sys.exit("Field can only be avg_angularError or stdev_angularError")
    master = Tk()
    master.title("Data Visualisation")

    canvas_width = 1000
    canvas_height = 800
    center_x = canvas_width/2
    center_y = canvas_height/2
    t1_radius = 150
    t2_radius = 300
    a= math.pi / 6
    b= math.pi / 3
    t1_x1 = center_x + t1_radius*(math.cos(2*b))
    t1_x2 = center_x + t1_radius*(math.cos(b))
    t1_y1 = center_y - t1_radius*(math.sin(b))
    t1_y2 = center_y - t1_radius*(math.sin(-b))
    t2_x1 = center_x + t2_radius*(math.cos(5*a))
    t2_x2 = center_x + t2_radius*(math.cos(a))
    t2_y1 = center_y - t2_radius*(math.sin(a))
    t2_y2 = center_y - t2_radius*(math.sin(-a))
    w = Canvas(master, width=canvas_width, height=canvas_height, background="#F4F4F6")
    w.pack()

    w.create_text((53, 15), text="Depth: " + str(depth), font="fixedsys 25", fill="#011424")
    w.create_text((130, 45), text="Field: " + field, font="fixedsys 25", fill="#011424")

    with open(stat_file, mode='r') as csv_file_in:
        csv_reader = csv.DictReader(csv_file_in)
        emptyDictionary(sphere_lists)
        for row in csv_reader:
            if(row[field] == "N/A"):
                switch_sphere(sphere_lists, row['sphere_label'], row[field])
            else:
                switch_sphere(sphere_lists, row['sphere_label'], float(row[field]))

    label_list = []
    t1_s1 = circle_create(w, center_x, center_y, "t1_s1", label_list) # t1_s1
    t1_s7 = circle_create(w, t1_x1, t1_y1, "t1_s7", label_list)
    t1_s2 = circle_create(w, t1_x1, t1_y2, "t1_s2", label_list)
    t1_s6 = circle_create(w, t1_x2, t1_y1, "t1_s6", label_list)
    t1_s5 = circle_create(w, t1_x2, t1_y2, "t1_s5", label_list)
    t1_s3 = circle_create(w, (center_x-t1_radius), center_y, "t1_s3", label_list)
    t1_s4 = circle_create(w, (center_x+t1_radius), center_y, "t1_s4", label_list)
    t2_s1 = circle_create(w, t2_x1, t2_y1, "t2_s7", label_list)
    t2_s3 = circle_create(w, t2_x1, t2_y2, "t2_s3", label_list)
    t2_s4 = circle_create(w, t2_x2, t2_y1, "t2_s4", label_list)
    t2_s5 = circle_create(w, t2_x2, t2_y2, "t2_s5", label_list)
    t2_s6 = circle_create(w, center_x, center_y-t2_radius, "t2_s6", label_list)
    t2_s2 = circle_create(w, center_x, center_y+t2_radius, "t2_s2", label_list)

    for sphere_vis in label_list:
        sphereLis = sphere_lists.get(w.itemcget(sphere_vis, "text") + "_" + str(depth))
        val = sphereLis[0]
        if(val == "N/A"):
            w.itemconfig(sphere_vis, text = val)
        else:
            w.itemconfig(sphere_vis, text = round(val,3))

    mainloop()
