public static class NameGenerator
{
    private static readonly string[] MaleFirstNames =
    {
        "James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Charles",
        "Christopher", "Daniel", "Matthew", "Anthony", "Mark", "Donald", "Steven", "Paul", "Andrew", "Joshua",
        "Kenneth", "Kevin", "Brian", "George", "Timothy", "Ronald", "Edward", "Jason", "Jeffrey", "Ryan",
        "Jacob", "Gary", "Nicholas", "Eric", "Jonathan", "Stephen", "Larry", "Justin", "Scott", "Brandon",
        "Benjamin", "Samuel", "Raymond", "Gregory", "Frank", "Alexander", "Patrick", "Jack", "Dennis", "Jerry",
        "Tyler", "Aaron", "Nathan", "Henry", "Peter", "Adam", "Douglas", "Zachary", "Walter", "Kyle",
        "Harold", "Carl", "Jeremy", "Gerald", "Keith", "Roger", "Arthur", "Terry", "Lawrence", "Jesse",
        "Dylan", "Bryan", "Joe", "Marcus", "Oscar", "Alan", "Vincent", "Juan", "Gabriel", "Sean",
        "Ethan", "Logan", "Lucas", "Mason", "Liam", "Noah", "Owen", "Caleb", "Connor", "Adrian",
        "Ian", "Evan", "Colin", "Sebastian", "Xavier", "Dominic", "Julian", "Leo", "Miles", "Felix",
        "Carlos", "Miguel", "Diego", "Alejandro", "Rafael", "Fernando", "Pablo", "Enrique", "Andres", "Luis",
        "Marco", "Ricardo", "Eduardo", "Sergio", "Javier", "Roberto", "Hugo", "Manuel", "Santiago", "Mateo",
        "Hiroshi", "Takeshi", "Kenji", "Yuki", "Ryo", "Haruto", "Ren", "Sora", "Kaito", "Daiki",
        "Wei", "Ming", "Jun", "Chen", "Hao", "Liang", "Feng", "Tao", "Lei", "Bo",
        "Raj", "Arjun", "Vikram", "Anil", "Sanjay", "Amit", "Rahul", "Deepak", "Suresh", "Ravi",
        "Kwame", "Emeka", "Oluwaseun", "Tendai", "Jabari", "Kofi", "Idris", "Amadi", "Chidi", "Obinna",
        "Dmitri", "Alexei", "Nikolai", "Ivan", "Sergei", "Vladimir", "Andrei", "Pavel", "Mikhail", "Oleg",
        "Nils", "Sven", "Lars", "Erik", "Aksel", "Magnus", "Bjorn", "Leif", "Gustaf", "Anders",
        "Omar", "Hassan", "Karim", "Ali", "Youssef", "Tariq", "Nabil", "Samir", "Khalil", "Rashid",
        "Luca", "Matteo", "Giovanni", "Alessandro", "Lorenzo", "Fabio", "Stefano", "Pietro", "Simone", "Tommaso"
    };

    private static readonly string[] FemaleFirstNames =
    {
        "Mary", "Patricia", "Jennifer", "Linda", "Barbara", "Elizabeth", "Susan", "Jessica", "Sarah", "Karen",
        "Lisa", "Nancy", "Betty", "Margaret", "Sandra", "Ashley", "Dorothy", "Kimberly", "Emily", "Donna",
        "Michelle", "Carol", "Amanda", "Melissa", "Deborah", "Stephanie", "Rebecca", "Sharon", "Laura", "Cynthia",
        "Kathleen", "Amy", "Angela", "Shirley", "Anna", "Brenda", "Pamela", "Emma", "Nicole", "Helen",
        "Samantha", "Katherine", "Christine", "Debra", "Rachel", "Carolyn", "Janet", "Catherine", "Maria", "Heather",
        "Diane", "Ruth", "Julie", "Olivia", "Joyce", "Virginia", "Victoria", "Kelly", "Lauren", "Christina",
        "Joan", "Evelyn", "Judith", "Megan", "Andrea", "Cheryl", "Hannah", "Jacqueline", "Martha", "Gloria",
        "Teresa", "Ann", "Sara", "Madison", "Frances", "Kathryn", "Janice", "Jean", "Abigail", "Alice",
        "Sophia", "Isabella", "Mia", "Charlotte", "Amelia", "Harper", "Ella", "Aria", "Grace", "Chloe",
        "Lily", "Eleanor", "Violet", "Stella", "Natalie", "Zoe", "Leah", "Hazel", "Aurora", "Savannah",
        "Sofia", "Valentina", "Camila", "Lucia", "Elena", "Gabriela", "Mariana", "Isabel", "Daniela", "Carolina",
        "Rosa", "Carmen", "Alejandra", "Fernanda", "Adriana", "Paula", "Diana", "Claudia", "Beatriz", "Catalina",
        "Yuki", "Sakura", "Hana", "Aiko", "Mei", "Haruka", "Rina", "Yui", "Miku", "Saki",
        "Lin", "Xia", "Jing", "Hua", "Yan", "Fang", "Qian", "Ying", "Lan", "Zhen",
        "Priya", "Anita", "Sunita", "Kavita", "Neha", "Pooja", "Divya", "Asha", "Meera", "Lakshmi",
        "Amara", "Zuri", "Nia", "Imani", "Adaeze", "Chioma", "Fatima", "Aisha", "Nkechi", "Yewande",
        "Olga", "Natasha", "Irina", "Svetlana", "Tatiana", "Anastasia", "Ekaterina", "Marina", "Alena", "Vera",
        "Ingrid", "Astrid", "Freya", "Sigrid", "Elsa", "Greta", "Helga", "Maja", "Linnea", "Saga",
        "Layla", "Yasmin", "Nadia", "Samira", "Leila", "Amina", "Zara", "Dina", "Rania", "Soraya",
        "Giulia", "Francesca", "Chiara", "Bianca", "Eleonora", "Martina", "Alessia", "Serena", "Ilaria", "Paola"
    };

    private static readonly string[] NeutralFirstNames =
    {
        "Alex", "Jordan", "Morgan", "Taylor", "Casey", "Riley", "Avery", "Quinn", "Sage", "Dakota",
        "Charlie", "Skyler", "Blake", "Rowan", "River", "Jamie", "Phoenix", "Kai", "Drew", "Cameron",
        "Reese", "Kendall", "Ashton", "Parker", "Sawyer", "Emery", "Finley", "Hayden", "Remy", "Marlowe",
        "Arden", "Ellis", "Lennox", "Shiloh", "Wren", "Briar", "Harley", "Indie", "Oakley", "Tatum",
        "Rory", "Eden", "Sasha", "Jesse", "Robin", "Kerry", "Dana", "Pat", "Kim", "Lee"
    };

    private static readonly string[] LastNames =
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
        "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
        "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson",
        "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
        "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell", "Carter", "Roberts",
        "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker", "Cruz", "Edwards", "Collins", "Reyes",
        "Stewart", "Morris", "Morales", "Murphy", "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper",
        "Peterson", "Bailey", "Reed", "Kelly", "Howard", "Ramos", "Kim", "Cox", "Ward", "Richardson",
        "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray", "Mendoza", "Ruiz", "Hughes",
        "Price", "Alvarez", "Castillo", "Sanders", "Patel", "Myers", "Long", "Ross", "Foster", "Jimenez",
        "Tanaka", "Yamamoto", "Nakamura", "Watanabe", "Suzuki", "Sato", "Kobayashi", "Takahashi", "Ito", "Saito",
        "Chen", "Wang", "Zhang", "Liu", "Yang", "Huang", "Wu", "Zhou", "Li", "Zhao",
        "Sharma", "Gupta", "Singh", "Kumar", "Mehta", "Shah", "Joshi", "Desai", "Rao", "Kapoor",
        "Okafor", "Osei", "Mensah", "Diallo", "Mwangi", "Okello", "Adeyemi", "Mbeki", "Nwosu", "Abubakar",
        "Volkov", "Petrov", "Sokolov", "Kuznetsov", "Popov", "Novak", "Horvat", "Kovalenko", "Babic", "Mazur"
    };

    public static string GenerateRandomName(IRng rng, Gender gender)
    {
        string[] pool;
        switch (gender)
        {
            case Gender.Male:
                pool = MaleFirstNames;
                break;
            case Gender.Female:
                pool = FemaleFirstNames;
                break;
            default:
                pool = NeutralFirstNames;
                break;
        }

        string firstName = pool[rng.Range(0, pool.Length)];
        string lastName = LastNames[rng.Range(0, LastNames.Length)];
        return firstName + " " + lastName;
    }

    public static string GenerateRandomName(IRng rng)
    {
        int genderIndex = rng.Range(0, 2);
        Gender gender = (Gender)genderIndex;
        return GenerateRandomName(rng, gender);
    }
}
